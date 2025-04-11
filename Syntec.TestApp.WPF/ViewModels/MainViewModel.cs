using System;
using ReactiveUI;
using Syntec.Remote;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Reactive.Concurrency;
using Syntec.TestApp.WPF.Utils;
using Syntec.TestApp.WPF.Log;
using System.Reactive.Disposables;

namespace Syntec.TestApp.WPF.ViewModels
{
    public class MainViewModel : ReactiveBaseModel
    {
        private string _host = "127.0.0.1";
        private int _timeout = 5000;
        private bool _isConnected;
        private string _statusMessage = "Не подключено";

        // Модели данных
        
        //Общая информация о ЧПУ
        public CncInfo CncInfo { get; }

        //Текущее состояние ЧПУ
        public CncStatus CncStatus { get; }
        
        //Текущие координаты по осям ЧПУ
        public AxisCoordinates AxisCoordinates { get; }

        public LogViewModel LogViewModel { get; }

        // Команды

        // Команда для подключения к ЧПУ 
        public ReactiveCommand<Unit, Unit> ConnectCommand { get; }

        // Команда для отключения от ЧПУ
        public ReactiveCommand<Unit, Unit> DisconnectCommand { get; }

        // Команда для запуска опроса 
        public ReactiveCommand<Unit, Unit> StartPollingCommand { get; }

        // Команда для остановки опроса 
        public ReactiveCommand<Unit, Unit> StopPollingCommand { get; }

        

        // Свойства
        public string Host
        {
            get => _host;
            set => this.RaiseAndSetIfChanged(ref _host, value);
        }

        public int Timeout
        {
            get => _timeout;
            set => this.RaiseAndSetIfChanged(ref _timeout, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            private set => this.RaiseAndSetIfChanged(ref _isConnected, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        // Реализация IScreen для навигации
        public RoutingState Router { get; } = new RoutingState();

        public MainViewModel()
        {      
            //
            var cncConnection = new SyntecRemoteCNC(Host, Timeout);


            SubscribeToNotifications(this);

            // Инициализация моделей
            LogViewModel      = new LogViewModel();

            CncInfo           = new CncInfo(cncConnection);
            CncStatus         = new CncStatus(cncConnection);
            AxisCoordinates   = new AxisCoordinates(cncConnection);
            

            // Подписка на уведомления от моделей
            SubscribeToNotifications(CncInfo);
            SubscribeToNotifications(CncStatus);
            SubscribeToNotifications(AxisCoordinates);

            // Настройка команд

            // Создаем observables для условий возможности выполнения команд
            var canConnect = this.WhenAnyValue(x => x.IsConnected)
                               .Select(connected => !connected);

            var canDisconnect = this.WhenAnyValue(x => x.IsConnected);

            var canStartPolling = this.WhenAnyValue(
                x => x.IsConnected,
                x => x.AxisCoordinates.IsPolling,
                (connected, polling) => connected && !polling);

            var canStopPolling = this.WhenAnyValue(x => x.AxisCoordinates.IsPolling);

            // Настройка команд с параметрами canExecute
            ConnectCommand      = ReactiveCommand.CreateFromTask(ConnectAsync, canConnect);
            DisconnectCommand   = ReactiveCommand.Create(Disconnect, canDisconnect);

            StartPollingCommand = ReactiveCommand.CreateFromTask(StartPollingAsync, canStartPolling);
            StopPollingCommand  = ReactiveCommand.Create(StopPolling, canStopPolling);            
        }

        private void SubscribeToNotifications(ReactiveBaseModel model)
        {
            model.Notifications
                .ObserveOn(RxApp.MainThreadScheduler) // Для UI-операций
                .Subscribe(args =>
                {
                    LogViewModel.AddEntry(new LogEntry(
                        args.Message,
                        args.Type,
                        model.GetType().Name.Replace("ViewModel", "")));
                })
                .DisposeWith(Disposables);
        }


        private async Task ConnectAsync()
        {
            try
            {
                StatusMessage = "Подключение...";
                var cnc = new SyntecRemoteCNC(Host, Timeout);
                IsConnected = await Task.Run(() => cnc.isConnected());

                if (IsConnected)
                {
                    CncInfo.RemoteCnc = cnc;
                    CncStatus.RemoteCnc = cnc;
                    AxisCoordinates.RemoteCnc = cnc;
                    StatusMessage = $"Подключено к {Host}";
                }
                else
                {
                    StatusMessage = "Ошибка подключения";
                    OnNotification($"Не удалось подключиться к ЧПУ ({Host})", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                StatusMessage = "Ошибка подключения";
                OnNotification($"Ошибка подключения: {ex.Message}", NotificationType.Error);
            }
        }

        private void Disconnect()
        {
            try
            {
                AxisCoordinates.StopPolling();
                CncInfo.RemoteCnc?.Dispose();
                CncStatus.RemoteCnc = null;
                AxisCoordinates.RemoteCnc = null;
                IsConnected = false;
                StatusMessage = "Отключено";
            }
            catch (Exception ex)
            {
                OnNotification($"Ошибка отключения: {ex.Message}", NotificationType.Error);
            }
        }

        private async Task StartPollingAsync()
        {
            await AxisCoordinates.StartPollingAsync(TimeSpan.FromSeconds(1));
        }

        private void StopPolling()
        {
            AxisCoordinates.StopPolling();
        }
    }
}