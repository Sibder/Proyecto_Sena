namespace Sena_app.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public int? IdUser { get; set; }   // NULL = global, sirve para que los usuarios puedan tener categorías globales y personales

        // ── Navegación EF Core ───────────────────────────────────────────────
        public User? User { get; set; }
        public List<TaskItem> Tasks { get; set; } = new();
    }
}
