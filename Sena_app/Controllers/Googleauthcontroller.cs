using Microsoft.AspNetCore.Mvc;
using Sena_app.Data;
using Sena_app.Models;
using Sena_app.Services;
using Microsoft.EntityFrameworkCore;

namespace Sena_app.Controllers
{
    [ApiController]
    [Route("auth/google")]
    public class GoogleAuthController : Controller
    {
        private readonly GoogleCalendarService _calendarService;
        private readonly IDbContextFactory<AppDbContext> _factory;

        public GoogleAuthController(GoogleCalendarService calendarService,
                                    IDbContextFactory<AppDbContext> factory)
        {
            _calendarService = calendarService;
            _factory = factory;
        }

        // ── Callback de Google Calendar OAuth ─────────────────────────────────
        [HttpGet("callback")]
        public async Task<IActionResult> Callback(
            [FromQuery] string code,
            [FromQuery] string state,
            [FromQuery] string? error)
        {
            if (!string.IsNullOrEmpty(error))
                return Redirect($"/profile?calendarError={error}");

            // Si state == "signin" → es un Sign-In, no Calendar, esto es para iniciar sesión con Google usando las mismas credenciales de OAuth
            if (state == "signin")
                return await HandleSignIn(code);

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                return Redirect("/profile?calendarError=invalid");

            if (!int.TryParse(state, out int userId))
                return Redirect("/profile?calendarError=invalid");

            bool ok = await _calendarService.ExchangeCodeAsync(code, userId);

            return ok
                ? Redirect("/profile?calendarConnected=true")
                : Redirect("/profile?calendarError=token");
        }

        // ── Iniciar Sign-In con Google ────────────────────────────────────────
        [HttpGet("signin")]
        public IActionResult SignIn()
        {
            var url = _calendarService.GetSignInUrl();
            return Redirect(url);
        }

        // ── Manejar callback de Sign-In ───────────────────────────────────────
        private async Task<IActionResult> HandleSignIn(string code)
        {
            var (email, firstName, lastName) = await _calendarService.GetUserProfileAsync(code);

            if (email is null)
                return Redirect("/?googleError=perfil");

            using var db = _factory.CreateDbContext();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user is null)
            {
                // Crear cuenta automáticamente
                user = new User
                {
                    FirstName = firstName ?? "Usuario",
                    LastName = lastName ?? "Google",
                    Email = email,
                    Password = Guid.NewGuid().ToString(), // password aleatorio
                    CreatedAt = DateTime.Now
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }

            // Guardar el Id en la sesión via query param cifrado
            // El Index.razor lo lee y hace login
            return Redirect($"/?googleLogin={user.Id}");
        }
    }
}