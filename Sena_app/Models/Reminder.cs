namespace Sena_app.Models
{
    public enum NotifType { Email, Push, Both }
    public enum ReminderFreq { Once, Daily, Weekly }
    public enum ReminderStatus { Pending, Sent, Postponed }
    public class Reminder
    {
        public int Id { get; set; }
        public DateOnly SendDate { get; set; }
        public TimeOnly SendHour { get; set; }
        public NotifType TypeNotf { get; set; } = NotifType.Email;
        public ReminderFreq Frequency { get; set; } = ReminderFreq.Once;
        public ReminderStatus State { get; set; } = ReminderStatus.Pending;
        public int MinutePost { get; set; } = 0;
        public int IdTask { get; set; }

        // ── Navegación EF Core ───────────────────────────────────────────────
        public TaskItem? Task { get; set; }
    }
}
