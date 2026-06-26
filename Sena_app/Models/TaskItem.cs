namespace Sena_app.Models
{
    public enum TaskPriority { Low, Medium, High }
    public enum ItemStatus { Pending, Done }

    // Enum de compatibilidad con el front — los valores coinciden
    // con los nombres de las categorías globales del SEED en la BD.
    public enum TaskCategory { Work, Study, Home, Health }

    /// <summary>
    /// Representa una tarea en Taskly.
    /// Se llama TaskItem porque "Task" es palabra reservada en C#.
    /// Mapeado a la tabla Task en SQL Server.
    /// </summary>
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateOnly? LimitDate { get; set; }   // limit_date en BD
        public TimeOnly? LimitHour { get; set; }   // limit_hour en BD
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public ItemStatus Status { get; set; } = ItemStatus.Pending;
        public int IdUser { get; set; }   // FK → User en BD
        public int? IdCategory { get; set; }   // FK → Category en BD
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? GoogleEventId { get; set; }

        // ── Propiedades de compatibilidad con el front ───────────────────────

        /// <summary>
        /// UserId — alias de IdUser para compatibilidad con el front.
        /// El front usa task.UserId al crear tareas (Home.razor línea 746).
        /// </summary>
        public int UserId
        {
            get => IdUser;
            set => IdUser = value;
        }

        /// <summary>
        /// DueDate — combina LimitDate y LimitHour para el front.
        /// El front lo usa para mostrar fechas y para asignar al editar.
        /// </summary>
        public DateTime DueDate
        {
            get => LimitDate.HasValue
                ? LimitDate.Value.ToDateTime(LimitHour ?? TimeOnly.MinValue)
                : DateTime.Now.AddDays(1);
            set
            {
                LimitDate = DateOnly.FromDateTime(value);
                LimitHour = TimeOnly.FromDateTime(value);
            }
        }

        /// <summary>
        /// Category como TaskCategory — el front la compara con el enum
        /// (t.Category == TaskCategory.Work, etc.) y la asigna al crear/editar.
        /// Se mapea desde/hacia IdCategory usando los IDs del SEED:
        ///   Work=1, Study=2, Home=3, Health=4 (orden del INSERT en el DDL).
        /// </summary>
        public TaskCategory Category
        {
            get => IdCategory switch
            {
                2 => TaskCategory.Study,
                3 => TaskCategory.Home,
                4 => TaskCategory.Health,
                _ => TaskCategory.Work    // 1 o null → Work
            };
            set => IdCategory = value switch
            {
                TaskCategory.Study => 2,
                TaskCategory.Home => 3,
                TaskCategory.Health => 4,
                _ => 1  // Work
            };
        }

        // ── Propiedades calculadas de estado ─────────────────────────────────
        public bool IsDone => Status == ItemStatus.Done;
        public bool IsOverdue => DueDate.Date < DateTime.Today && !IsDone;
        public bool IsDueToday => DueDate.Date == DateTime.Today && !IsDone;

        // ── Navegación EF Core ───────────────────────────────────────────────
        public User? User { get; set; }
        public Category? CategoryNav { get; set; }
        public Reminder? Reminder { get; set; }
    }
}
