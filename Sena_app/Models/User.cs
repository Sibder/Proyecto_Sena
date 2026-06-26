namespace Sena_app.Models
{
    /// <summary>
    /// Representa un usuario de la aplicación Taskly.
    /// Mapeado a la tabla [User] en SQL Server.
    /// </summary>
    public class User
    {
        public int Id { get; set; }   // id_user
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // bcrypt en producción
        public string? Phone { get; set; }
        public string? PrefNotf { get; set; }
        public string? PrefView { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? GoogleAccessToken { get; set; }
        public string? GoogleRefreshToken { get; set; }
        public DateTime? GoogleTokenExpiry { get; set; }
        /// <summary>True si la cuenta fue creada via Google Sign-In.</summary>
        public bool IsGoogleAccount => Password.Length == 36 && Password.Contains('-');

        // ── Propiedades calculadas (no van a la BD) ──────────────────────────
        public string FullName => $"{FirstName} {LastName}";
        public string Initials =>
            $"{(FirstName.Length > 0 ? FirstName[0] : ' ')}" +
            $"{(LastName.Length > 0 ? LastName[0] : ' ')}".ToUpper();

        /// <summary>
        /// Plan se mantiene para no romper el front.
        /// Siempre retorna "Free" hasta que se implemente la lógica de planes.
        /// </summary>
        public string Plan => "Free";

        // ── Navegación EF Core ───────────────────────────────────────────────
        public List<TaskItem> Tasks { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }
}
