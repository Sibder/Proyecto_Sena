using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Sena_app.Data;
using Sena_app.Models;

namespace Sena_app.Services
{
    public class GoogleCalendarService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IConfiguration _config;

        public GoogleCalendarService(IDbContextFactory<AppDbContext> factory,
                                     IConfiguration config)
        {
            _factory = factory;
            _config = config;
        }

        // ── Flujo OAuth base ──────────────────────────────────────────────────
        private GoogleAuthorizationCodeFlow BuildFlow(string[] scopes)
        {
            return new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = _config["GoogleCalendar:ClientId"],
                        ClientSecret = _config["GoogleCalendar:ClientSecret"]
                    },
                    Scopes = scopes
                });
        }

        // ── URL de autorización para Google Calendar ──────────────────────────
        public string GetAuthorizationUrl(string state)
        {
            var flow = BuildFlow(new[] { CalendarService.Scope.Calendar });
            var redirectUri = _config["GoogleCalendar:RedirectUri"];
            return flow.CreateAuthorizationCodeRequest(redirectUri)
                       .Build().AbsoluteUri + $"&state={state}";
        }

        // ── URL de autorización para Google Sign-In ───────────────────────────
        public string GetSignInUrl()
        {
            var flow = BuildFlow(new[]
            {
                "openid",
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/userinfo.profile"
            });
            var redirectUri = _config["GoogleCalendar:SignInRedirectUri"];
            return flow.CreateAuthorizationCodeRequest(redirectUri)
                       .Build().AbsoluteUri + "&state=signin";
        }

        // ── Intercambiar código por perfil de usuario (Sign-In) ───────────────
        public async Task<(string? Email, string? FirstName, string? LastName)>
            GetUserProfileAsync(string code)
        {
            var flow = BuildFlow(new[]
            {
                "openid",
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/userinfo.profile"
            });
            var redirectUri = _config["GoogleCalendar:SignInRedirectUri"];

            var token = await flow.ExchangeCodeForTokenAsync(
                "signin", code, redirectUri, CancellationToken.None);

            if (token?.IdToken is null)
                return (null, null, null);

            var payload = await GoogleJsonWebSignature.ValidateAsync(token.IdToken);

            var fullName = payload.Name?.Split(' ') ?? Array.Empty<string>();
            var firstName = fullName.Length > 0 ? fullName[0] : "Usuario";
            var lastName = fullName.Length > 1
                ? string.Join(" ", fullName.Skip(1))
                : "Google";

            return (payload.Email, firstName, lastName);
        }

        // ── Intercambiar código por token de Calendar ─────────────────────────
        public async Task<bool> ExchangeCodeAsync(string code, int userId)
        {
            var flow = BuildFlow(new[] { CalendarService.Scope.Calendar });
            var redirectUri = _config["GoogleCalendar:RedirectUri"];

            var token = await flow.ExchangeCodeForTokenAsync(
                userId.ToString(), code, redirectUri, CancellationToken.None);

            if (token is null) return false;

            using var db = _factory.CreateDbContext();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return false;

            user.GoogleAccessToken = token.AccessToken;
            user.GoogleRefreshToken = token.RefreshToken;
            user.GoogleTokenExpiry = token.IssuedUtc.AddSeconds(token.ExpiresInSeconds ?? 3600);
            await db.SaveChangesAsync();

            return true;
        }

        // ── Construir CalendarService autenticado ─────────────────────────────
        private async Task<CalendarService?> BuildCalendarServiceAsync(int userId)
        {
            using var db = _factory.CreateDbContext();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.GoogleAccessToken is null) return null;

            var flow = BuildFlow(new[] { CalendarService.Scope.Calendar });

            var token = new TokenResponse
            {
                AccessToken = user.GoogleAccessToken,
                RefreshToken = user.GoogleRefreshToken,
                ExpiresInSeconds = (long)(user.GoogleTokenExpiry - DateTime.UtcNow)
                                   .GetValueOrDefault().TotalSeconds
            };

            var credential = new UserCredential(flow, userId.ToString(), token);

            // Si el token expiró, renovarlo automáticamente con el refresh token
            if (user.GoogleTokenExpiry.HasValue && user.GoogleTokenExpiry.Value <= DateTime.UtcNow)
            {
                var newToken = await credential.RefreshTokenAsync(CancellationToken.None);
                if (newToken)
                {
                    // Guardar el nuevo token en la BD
                    using var dbUpdate = _factory.CreateDbContext();
                    var userToUpdate = await dbUpdate.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (userToUpdate != null)
                    {
                        userToUpdate.GoogleAccessToken = credential.Token.AccessToken;
                        userToUpdate.GoogleTokenExpiry = credential.Token.IssuedUtc
                            .AddSeconds(credential.Token.ExpiresInSeconds ?? 3600);
                        await dbUpdate.SaveChangesAsync();
                    }
                }
            }

            return new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Taskly"
            });
        }

        // ── Crear evento ──────────────────────────────────────────────────────
        public async Task<string?> CreateEventAsync(int userId, TaskItem task)
        {
            var service = await BuildCalendarServiceAsync(userId);
            if (service is null) return null;

            var start = task.LimitDate.HasValue
                ? task.LimitDate.Value.ToDateTime(task.LimitHour ?? new TimeOnly(9, 0))
                : DateTime.Now.AddDays(1);

            var calEvent = new Event
            {
                Summary = task.Title,
                Description = task.Description,
                Start = new EventDateTime
                {
                    DateTimeDateTimeOffset = new DateTimeOffset(start),
                    TimeZone = "America/Bogota"
                },
                End = new EventDateTime
                {
                    DateTimeDateTimeOffset = new DateTimeOffset(start.AddHours(1)),
                    TimeZone = "America/Bogota"
                },
                Reminders = new Event.RemindersData
                {
                    UseDefault = false,
                    Overrides = new List<EventReminder>
                    {
                        new EventReminder { Method = "popup", Minutes = 30 }
                    }
                }
            };

            var created = await service.Events.Insert(calEvent, "primary").ExecuteAsync();
            return created.Id;
        }

        // ── Actualizar evento ─────────────────────────────────────────────────
        public async Task UpdateEventAsync(int userId, string googleEventId, TaskItem task)
        {
            var service = await BuildCalendarServiceAsync(userId);
            if (service is null) return;

            var start = task.LimitDate.HasValue
                ? task.LimitDate.Value.ToDateTime(task.LimitHour ?? new TimeOnly(9, 0))
                : DateTime.Now.AddDays(1);

            var calEvent = new Event
            {
                Summary = task.Title,
                Description = task.Description,
                Start = new EventDateTime
                {
                    DateTimeDateTimeOffset = new DateTimeOffset(start),
                    TimeZone = "America/Bogota"
                },
                End = new EventDateTime
                {
                    DateTimeDateTimeOffset = new DateTimeOffset(start.AddHours(1)),
                    TimeZone = "America/Bogota"
                }
            };

            await service.Events.Update(calEvent, "primary", googleEventId).ExecuteAsync();
        }

        // ── Eliminar evento ───────────────────────────────────────────────────
        public async Task DeleteEventAsync(int userId, string googleEventId)
        {
            var service = await BuildCalendarServiceAsync(userId);
            if (service is null) return;
            await service.Events.Delete("primary", googleEventId).ExecuteAsync();
        }

        // ── Verificar conexión ────────────────────────────────────────────────
        public async Task<bool> IsConnectedAsync(int userId)
        {
            using var db = _factory.CreateDbContext();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return user?.GoogleAccessToken is not null;
        }
    }
}