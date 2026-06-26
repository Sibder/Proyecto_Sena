using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using Sena_app.Data;
using Sena_app.Models;

namespace Sena_app.Services
{
    public class AuthService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ProtectedSessionStorage _storage;
        private const string SESSION_KEY = "taskly_uid";

        public User? CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;

        public AuthService(IDbContextFactory<AppDbContext> factory,
                           ProtectedSessionStorage storage)
        {
            _factory = factory;
            _storage = storage;
        }

        // ── Iniciar sesión con google ───────────────────────────
        public async Task<bool> LoginByIdAsync(int userId)
        {
            using var db = _factory.CreateDbContext();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return false;

            CurrentUser = user;
            await _storage.SetAsync(SESSION_KEY, user.Id);
            return true;
        }

        // ── Restaurar sesión al iniciar el circuito ───────────────────────────

        public async Task RestoreSessionAsync()
        {
            if (CurrentUser != null) return;

            try
            {
                var result = await _storage.GetAsync<int>(SESSION_KEY);
                if (!result.Success || result.Value == 0) return;

                using var db = _factory.CreateDbContext();
                CurrentUser = await db.Users.FirstOrDefaultAsync(u => u.Id == result.Value);
            }
            catch
            {
            }
        }

        // ── Login ─────────────────────────────────────────────────────────────
        public async Task<bool> LoginAsync(string email, string password)
        {
            using var db = _factory.CreateDbContext();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.Password)) return false; ;

            CurrentUser = user;
            await _storage.SetAsync(SESSION_KEY, user.Id);
            return true;
        }

        // ── Registro ──────────────────────────────────────────────────────────
        public bool Register(string firstName, string lastName, string email, string password)
        {
            using var db = _factory.CreateDbContext();

            if (db.Users.Any(u => u.Email == email))
                return false;

            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.Now
            };

            db.Users.Add(user);
            db.SaveChanges();
            return true;
        }

        // ── Actualizar perfil ─────────────────────────────────────────────────
        public bool UpdateProfile(string firstName, string lastName, string email, string? phone)
        {
            if (CurrentUser is null) return false;

            using var db = _factory.CreateDbContext();
            var user = db.Users.FirstOrDefault(u => u.Id == CurrentUser.Id);
            if (user is null) return false;

            if (user.Email != email && db.Users.Any(u => u.Email == email && u.Id != CurrentUser.Id))
                return false;

            user.FirstName = firstName.Trim();
            user.LastName = lastName.Trim();
            user.Email = email.Trim();
            user.Phone = phone?.Trim();

            db.SaveChanges();

            CurrentUser.FirstName = user.FirstName;
            CurrentUser.LastName = user.LastName;
            CurrentUser.Email = user.Email;
            CurrentUser.Phone = user.Phone;

            return true;
        }

        // ── FindUserByEmail ───────────────────────────────────────────────────
        public User? FindUserByEmail(string email)
        {
            using var db = _factory.CreateDbContext();
            return db.Users.FirstOrDefault(u => u.Email == email);
        }

        // ── UpdatePassword ────────────────────────────────────────────────────
        public bool UpdatePassword(string email, string newPassword)
        {
            using var db = _factory.CreateDbContext();
            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user is null) return false;

            var hashed = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.Password = hashed;
            db.SaveChanges();
            if (CurrentUser?.Email == email)CurrentUser.Password = hashed;

            return true;
        }

        // ── Logout ────────────────────────────────────────────────────────────
        public async Task LogoutAsync()
        {
            CurrentUser = null;
            await _storage.DeleteAsync(SESSION_KEY);
        }
    }
}