using ReactiveUI;
using Syntec.Remote;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Syntec.TestApp.WPF.ViewModels
{
    /// <summary>
    /// Абстрактный базовый класс для всех моделей данных ЧПУ Syntec.
    /// Автоматически обновляет состояние при изменении подключения к ЧПУ.
    /// </summary>
    public abstract class ReactiveBaseModel : ReactiveObject, IDisposable
    {
        protected readonly CompositeDisposable Disposables = new CompositeDisposable();

        /// <summary>
        /// Событие для передачи информационных сообщений
        /// </summary>
        private readonly Subject<AppNotificationEventArgs> _notifications = new Subject<AppNotificationEventArgs>();
        public IObservable<AppNotificationEventArgs> Notifications => _notifications.AsObservable();


        /// <summary>
        /// Инициализирует модель с указанным подключением к ЧПУ.
        /// </summary>
        protected ReactiveBaseModel()
        {
        }

        /// <summary>
        /// Вызывает событие уведомления
        /// </summary>
        protected virtual void OnNotification(string message, NotificationType type = NotificationType.Info)
        {
            _notifications.OnNext(new AppNotificationEventArgs(message, type));
        }

        /// <summary>
        /// Освобождает все ресурсы, связанные с моделью.
        /// </summary>
        public void Dispose()
        {
            Disposables?.Dispose();
        }
    }


    /// <summary>
    /// Типы уведомлений
    /// </summary>
    public enum NotificationType
    {
        Debug,
        Info,
        Warning,
        Error        
    }


    /// <summary>
    /// Аргументы события уведомления
    /// </summary>
    public class AppNotificationEventArgs : EventArgs
    {
        public string Message { get; }
        public NotificationType Type { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public AppNotificationEventArgs(string message, NotificationType type)
        {
            Message = message;
            Type = type;
        }
    }
}