using Microsoft.EntityFrameworkCore;
using Sena_app.Data;
using Sena_app.Models;

namespace Sena_app.Services
{
    /// <summary>
    /// Servicio de recordatorios conectado a SQL Server via EF Core.
    /// Gestiona el CRUD de Reminder asociados 1:1 a una tarea.
    /// </summary>
    public class ReminderService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public ReminderService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        // ── Obtener recordatorio de una tarea ─────────────────────────────────
        public Reminder? GetByTask(int taskId)
        {
            using var db = _factory.CreateDbContext();
            return db.Reminders.FirstOrDefault(r => r.IdTask == taskId);
        }

        // ── Obtener todos los recordatorios pendientes de un usuario ──────────
        public List<Reminder> GetPendingByUser(int userId)
        {
            using var db = _factory.CreateDbContext();
            return db.Reminders
                     .Include(r => r.Task)
                     .Where(r => r.Task!.IdUser == userId &&
                                 r.State == ReminderStatus.Pending)
                     .ToList() // filtrar en memoria desde acá
                     .Where(r => r.SendDate <= DateOnly.FromDateTime(DateTime.Today) &&
                                 r.SendHour <= TimeOnly.FromDateTime(DateTime.Now))
                     .ToList();
        }

        // ── Crear o actualizar recordatorio de una tarea ──────────────────────
        /// <summary>
        /// Si la tarea ya tiene recordatorio lo actualiza,
        /// si no tiene lo crea. Nunca deja dos recordatorios por tarea.
        /// </summary>
        public void Save(Reminder reminder)
        {
            using var db = _factory.CreateDbContext();
            var existing = db.Reminders.FirstOrDefault(r => r.IdTask == reminder.IdTask);

            if (existing is null)
            {
                db.Reminders.Add(reminder);
            }
            else
            {
                existing.SendDate = reminder.SendDate;
                existing.SendHour = reminder.SendHour;
                existing.TypeNotf = reminder.TypeNotf;
                existing.Frequency = reminder.Frequency;
                existing.State = ReminderStatus.Pending;
                existing.MinutePost = 0;
            }

            db.SaveChanges();
        }

        // ── Eliminar recordatorio de una tarea ────────────────────────────────
        public void DeleteByTask(int taskId)
        {
            using var db = _factory.CreateDbContext();
            var reminder = db.Reminders.FirstOrDefault(r => r.IdTask == taskId);
            if (reminder is null) return;
            db.Reminders.Remove(reminder);
            db.SaveChanges();
        }

        // ── Marcar como enviado ───────────────────────────────────────────────
        public void MarkSent(int reminderId)
        {
            using var db = _factory.CreateDbContext();
            var reminder = db.Reminders.FirstOrDefault(r => r.Id == reminderId);
            if (reminder is null) return;
            reminder.State = ReminderStatus.Sent;
            db.SaveChanges();
        }

        // ── Verificar si una tarea tiene recordatorio ─────────────────────────
        public bool HasReminder(int taskId)
        {
            using var db = _factory.CreateDbContext();
            return db.Reminders.Any(r => r.IdTask == taskId);
        }
    }
}
