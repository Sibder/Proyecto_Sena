namespace Sena_app.Services
{
    /// <summary>
    /// Servicio Singleton que actúa como puente entre el ReminderBackgroundService
    /// y los componentes Blazor. Cuando el background detecta un recordatorio,
    /// dispara el evento OnNotification y el AppLayout lo muestra en pantalla.
    /// </summary>
    public class NotificationService
    {
        // Evento al que se suscriben los componentes
        public event Func<string, string, Task>? OnNotification;

        /// <summary>Dispara el popup en todos los circuitos suscritos.</summary>
        public async Task NotifyAsync(string title, string message)
        {
            if (OnNotification is not null)
                await OnNotification.Invoke(title, message);
        }
    }
}