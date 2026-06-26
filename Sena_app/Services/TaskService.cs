using Microsoft.EntityFrameworkCore;
using Sena_app.Data;
using Sena_app.Models;

namespace Sena_app.Services
{
    public class TaskService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly GoogleCalendarService _calendar;

        public TaskService(IDbContextFactory<AppDbContext> factory,
                           GoogleCalendarService calendar)
        {
            _factory = factory;
            _calendar = calendar;
        }

        // ── Consultas ─────────────────────────────────────────────────────────

        public List<TaskItem> GetByUser(int userId)
        {
            using var db = _factory.CreateDbContext();
            return db.Tasks
                     .Include(t => t.CategoryNav)
                     .Where(t => t.IdUser == userId)
                     .OrderBy(t => t.LimitDate)
                     .ToList();
        }

        public List<TaskItem> GetPending(int userId)
        {
            using var db = _factory.CreateDbContext();
            return db.Tasks
                     .Include(t => t.CategoryNav)
                     .Where(t => t.IdUser == userId && t.Status == ItemStatus.Pending)
                     .ToList();
        }

        public List<TaskItem> GetDone(int userId)
        {
            using var db = _factory.CreateDbContext();
            return db.Tasks
                     .Include(t => t.CategoryNav)
                     .Where(t => t.IdUser == userId && t.Status == ItemStatus.Done)
                     .ToList();
        }

        public List<TaskItem> GetToday(int userId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            using var db = _factory.CreateDbContext();
            return db.Tasks
                     .Include(t => t.CategoryNav)
                     .Where(t => t.IdUser == userId && t.LimitDate == today)
                     .ToList();
        }

        public int CountPending(int userId)
        {
            using var db = _factory.CreateDbContext();
            return db.Tasks.Count(t => t.IdUser == userId && t.Status == ItemStatus.Pending);
        }

        public int CountDone(int userId)
        {
            using var db = _factory.CreateDbContext();
            return db.Tasks.Count(t => t.IdUser == userId && t.Status == ItemStatus.Done);
        }

        public int CountTotal(int userId)
        {
            using var db = _factory.CreateDbContext();
            return db.Tasks.Count(t => t.IdUser == userId);
        }

        // ── Acciones ──────────────────────────────────────────────────────────

        public TaskItem Add(TaskItem task)
        {
            if (task is null)
                throw new ArgumentNullException(nameof(task));

            if (string.IsNullOrWhiteSpace(task.Title))
                throw new ArgumentException("El título de la tarea es obligatorio.");

            task.Title = task.Title.Trim();

            if (task.LimitDate.HasValue && task.LimitDate.Value < DateOnly.FromDateTime(DateTime.Today))
                throw new ArgumentException("La fecha límite no puede ser una fecha pasada.");

            using var db = _factory.CreateDbContext();

            bool duplicado = db.Tasks.Any(t =>
                t.IdUser == task.IdUser &&
                t.Status == ItemStatus.Pending &&
                t.Title.ToLower() == task.Title.ToLower());

            if (duplicado)
                throw new ArgumentException("Ya tienes una tarea pendiente con este título.");

            db.Tasks.Add(task);
            db.SaveChanges();

            // Sincronizar con Google Calendar si el usuario tiene cuenta conectada
            _ = SyncCreateAsync(task);

            return task;
        }

        public void Update(TaskItem task)
        {
            if (task is null)
                throw new ArgumentNullException(nameof(task));

            if (string.IsNullOrWhiteSpace(task.Title))
                throw new ArgumentException("El título de la tarea es obligatorio.");

            using var db = _factory.CreateDbContext();
            var existing = db.Tasks.FirstOrDefault(t => t.Id == task.Id);
            if (existing is null) return;

            existing.Title = task.Title.Trim();
            existing.Description = task.Description;
            existing.LimitDate = task.LimitDate;
            existing.LimitHour = task.LimitHour;
            existing.Priority = task.Priority;
            existing.IdCategory = task.IdCategory;

            db.SaveChanges();

            // Sincronizar con Google Calendar
            _ = SyncUpdateAsync(existing);
        }

        public void ToggleDone(int taskId)
        {
            using var db = _factory.CreateDbContext();
            var task = db.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task is null) return;

            task.Status = task.IsDone ? ItemStatus.Pending : ItemStatus.Done;
            db.SaveChanges();

            // Actualizar en Google Calendar
            _ = SyncUpdateAsync(task);
        }

        public void Delete(int taskId)
        {
            using var db = _factory.CreateDbContext();
            var task = db.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task is null) return;

            // Eliminar de Google Calendar antes de borrar de la BD
            if (!string.IsNullOrEmpty(task.GoogleEventId))
                _ = _calendar.DeleteEventAsync(task.IdUser, task.GoogleEventId);

            db.Tasks.Remove(task);
            db.SaveChanges();
        }

        // ── Sincronización Google Calendar (fire and forget) ──────────────────

        private async Task SyncCreateAsync(TaskItem task)
        {
            try
            {
                bool connected = await _calendar.IsConnectedAsync(task.IdUser);
                if (!connected) return;

                var eventId = await _calendar.CreateEventAsync(task.IdUser, task);
                if (eventId is null) return;

                // Guardar el Google Event ID en la BD
                using var db = _factory.CreateDbContext();
                var stored = db.Tasks.FirstOrDefault(t => t.Id == task.Id);
                if (stored is null) return;
                stored.GoogleEventId = eventId;
                db.SaveChanges();
            }
            catch
            {
                // No interrumpir el flujo si Google Calendar falla
            }
        }

        private async Task SyncUpdateAsync(TaskItem task)
        {
            try
            {
                if (string.IsNullOrEmpty(task.GoogleEventId)) return;
                bool connected = await _calendar.IsConnectedAsync(task.IdUser);
                if (!connected) return;

                await _calendar.UpdateEventAsync(task.IdUser, task.GoogleEventId, task);
            }
            catch
            {
                // No interrumpir el flujo si Google Calendar falla
            }
        }
    }
}