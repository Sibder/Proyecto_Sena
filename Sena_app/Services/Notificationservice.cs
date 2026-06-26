namespace Sena_app.Services
{
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