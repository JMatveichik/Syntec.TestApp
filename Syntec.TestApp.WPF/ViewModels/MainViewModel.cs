using System;
using ReactiveUI;
using Syntec.Remote;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Syntec.TestApp.WPF.ViewModels
{
    public class MainViewModel : ReactiveObject, IScreen
    {
        private string _host = "127.0.0.1";
        private int _timeout = 5000;
        private bool _isConnected;
        private string _statusMessage = "Не подключено";

        // Модели данных
        public CncInfo CncInfo { get; }
        public CncStatus CncStatus { get; }
        public AxisCoordinates AxisCoordinates { get; }

        // Команды
        public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
        public ReactiveCommand<Unit, Unit> DisconnectCommand { get; }
        public ReactiveCommand<Unit, Unit> StartPollingCommand { get; }
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

            // Инициализация моделей
            CncInfo           = new CncInfo(cncConnection);
            CncStatus         = new CncStatus(cncConnection);
            AxisCoordinates   = new AxisCoordinates(cncConnection);

            // Настройка команд
            // Создаем observables для условий выполнения команд
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
                    MessageBox.Show("Не удалось подключиться к ЧПУ", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                StatusMessage = "Ошибка подключения";
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Ошибка отключения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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