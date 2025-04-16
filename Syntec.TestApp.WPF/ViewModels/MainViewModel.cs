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
using System.Collections.ObjectModel;
using static RemoteFunctionModel;
using System.Linq;
using DynamicData.Binding;
using Splat;

namespace Syntec.TestApp.WPF.ViewModels
{
    public class MainViewModel : ReactiveBaseModel
    {
        private string _host = "192.168.88.99";
        private int _timeout = 5000;
        private bool _isConnected;
        private string _statusMessage = "Не подключено";
        private RemoteFunctionModel _selectedFunction = null;

        // Модели данных

        //Общая информация о ЧПУ
        public CncInfo CncInfo { get; }

        //Текущее состояние ЧПУ
        public CncStatus CncStatus { get; }
        
        //Текущие координаты по осям ЧПУ
        public AxisCoordinates AxisCoordinates { get; }

        public LogViewModel LogViewModel { get; }

        //Список моделей для вызова функций из библеотеки Syntec.RemoteCNC 
        public ObservableCollection<RemoteFunctionModel> Functions { get; } = new ObservableCollection<RemoteFunctionModel>();

        // Команды

        // Команда для подключения к ЧПУ 
        public ReactiveCommand<Unit, Unit> ConnectCommand { get; }

        // Команда для отключения от ЧПУ
        public ReactiveCommand<Unit, Unit> DisconnectCommand { get; }

        // Команда для запуска опроса 
        public ReactiveCommand<Unit, Unit> StartPollingCommand { get; }

        // Команда для остановки опроса 
        public ReactiveCommand<Unit, Unit> StopPollingCommand { get; }

        // Команда для обновления выбранных тегов 
        public ReactiveCommand<string, Unit> ToggleTagCommand { get; }

        public ReactiveCommand<Unit, Unit> ClearFilterCommand { get; }

        

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

        public RemoteFunctionModel SelectedFunction
        {
            get => _selectedFunction;
            set => this.RaiseAndSetIfChanged(ref _selectedFunction, value);
        }

        

        public MainViewModel()
        {      
            //
            var cncConnection = new SyntecRemoteCNC(Host, Timeout);


            SubscribeToNotifications(this);
            InitFunctionsList(cncConnection);
            InitFunctionsTags();

            // Инициализация моделей
            LogViewModel      = new LogViewModel();

            CncInfo           = new CncInfo(cncConnection);
            CncStatus         = new CncStatus(cncConnection);
            AxisCoordinates   = new AxisCoordinates(cncConnection);
            

            // Подписка на уведомления от моделей
            SubscribeToNotifications(CncInfo);
            SubscribeToNotifications(CncStatus);
            SubscribeToNotifications(AxisCoordinates);

            foreach(var model in Functions)
                SubscribeToNotifications(model);

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

            //------------------------------------------------------------------
            //Для фильтрации
            //------------------------------------------------------------------

            // Инициализация FilteredFunctions (вначале показываем все функции)
            FilteredFunctions = new ObservableCollection<RemoteFunctionModel>(Functions);

            // Команда для обновления фильтрации
            UpdateFilterCommand = ReactiveCommand.Create(UpdateFilter);

            //команда обновления тегов
            ToggleTagCommand = ReactiveCommand.Create<string>(tag => {
                if (SelectedTags.Contains(tag))
                    SelectedTags.Remove(tag);
                else
                    SelectedTags.Add(tag);
            });

            //очистка фильтра
            ClearFilterCommand = ReactiveCommand.Create(() =>
                {
                    SelectedTags.Clear();
                },

                this.WhenAnyValue(x => x.SelectedTags.Count)
                    .Select(count => count > 0)
            );

            // Подписка на изменения:
            SelectedTags
                .ToObservableChangeSet()
                .Throttle(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ => UpdateFilter());
        }

        //--------------------------------------------------------------------
        // Фильтрация функций по тегам
        //--------------------------------------------------------------------

        // выбранные теги
        private ObservableCollection<string> _selectedTags = new ObservableCollection<string>();
        // отфильтрованные функции
        private ObservableCollection<RemoteFunctionModel> _filteredFunctions = new ObservableCollection<RemoteFunctionModel>();

        /// <summary>
        /// Выбранные теги функций
        /// </summary>
        public ObservableCollection<string> SelectedTags
        {
            get => _selectedTags;
            set => this.RaiseAndSetIfChanged(ref _selectedTags, value);
        }

        /// <summary>
        /// Отфильтрованные функции
        /// </summary>
        public ObservableCollection<RemoteFunctionModel> FilteredFunctions
        {
            get => _filteredFunctions;
            private set => this.RaiseAndSetIfChanged(ref _filteredFunctions, value);
        }

        // Команда для обновления фильтрации
        public ReactiveCommand<Unit, Unit> UpdateFilterCommand { get; }


        // Метод для фильтрации функций
        private void UpdateFilter()
        {
            if (SelectedTags == null || !SelectedTags.Any())
            {
                // Если теги не выбраны - показываем все функции
                FilteredFunctions = new ObservableCollection<RemoteFunctionModel>(Functions);
                return;
            }

            // Фильтруем функции, которые содержат ВСЕ выбранные теги
            var filtered = Functions
                .Where(func => SelectedTags.All(tag => func.Tags.Contains(tag)))
                .ToList();

            FilteredFunctions = new ObservableCollection<RemoteFunctionModel>(filtered);
        }


        // Список для хранения всех уникальных тегов для фильтрации функций
        public ObservableCollection<string> AllTags { get; } = new ObservableCollection<string>();

        private void InitFunctionsTags()
        {
            // Собираем все теги из всех функций
            var allTags = Functions
                .SelectMany(f => f.Tags) // Выбираем все теги из всех функций
                .Distinct()              // Убираем дубликаты
                .OrderBy(t => t);        // Сортируем по алфавиту

            // Очищаем и заполняем коллекцию
            AllTags.Clear();
            foreach (var tag in allTags)
            {
                AllTags.Add(tag);
            }
        }

        /// <summary>
        /// Подписка модели на получение информационных сообщений 
        /// </summary>
        /// <param name="model"></param>
        private void SubscribeToNotifications(ReactiveBaseModel model)
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// Метод асинхронного подключения к ЧПУ
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Инициализация списка тестируемых функций
        /// </summary>
        /// <param name="cnc"></param>
        private void InitFunctionsList(SyntecRemoteCNC cnc)
        {
            Functions.Add(new ReadUseTime(cnc));
            Functions.Add(new ReadRemoteTimeFunction(cnc));
            Functions.Add(new WriteRemoteDateFunction(cnc));
            Functions.Add(new WriteRemoteTimeFunction(cnc));            
            Functions.Add(new ReadInformation(cnc));
            Functions.Add(new ReadStatusFunction(cnc));
            Functions.Add(new ReadPositionFunction(cnc));
            Functions.Add(new WriteRelPosFunction(cnc));
            Functions.Add(new ReadGCodeFunction(cnc));
            Functions.Add(new ReadOtherCodesFunction(cnc)); 
            Functions.Add(new ReadSpindleFunction(cnc));
            Functions.Add(new ReadMachineTimeFunction(cnc));
            Functions.Add(new DownloadWorkRecordFunction(cnc));
            Functions.Add(new ReadCurrentAlarmFunction(cnc));
            Functions.Add(new ReadAlarmHistoryFunction(cnc));
            Functions.Add(new ReadWorkCoordAxisFunction(cnc));
            Functions.Add(new ReadWorkCoordinatesScopeFunction(cnc));
            Functions.Add(new ReadWorkCoordinateSingleFunction(cnc));
            Functions.Add(new WriteWorkCoordinatesAllFunction(cnc));
            Functions.Add(new ReadMacroAllFunction(cnc));
            Functions.Add(new ReadMacroScopeFunction(cnc));
            Functions.Add(new ReadMacroSingleFunction(cnc));
            Functions.Add(new ReadMacroVariableFunction(cnc));
            Functions.Add(new WriteMacroAllFunction(cnc));
            Functions.Add(new WriteMacroSingleFunction(cnc));
            Functions.Add(new ReadParamDataFunction(cnc));
            Functions.Add(new ReadParamSchemaFunction(cnc));
            Functions.Add(new ReadPlcIbitFunction(cnc));
            Functions.Add(new ReadPlcObitFunction(cnc));
            Functions.Add(new ReadPlcCbitFunction(cnc));
            Functions.Add(new ReadPlcSbitFunction(cnc));
            Functions.Add(new ReadPlcAbitFunction(cnc));
            Functions.Add(new ReadPlcRegisterFunction(cnc));
            Functions.Add(new ReadPlcSingleRegisterFunction(cnc));
            Functions.Add(new ReadPlcTimerFunction(cnc));
            Functions.Add(new ReadPlcCounterFunction(cnc));
            Functions.Add(new WritePlcCbitFunction(cnc));
            Functions.Add(new WritePlcSbitFunction(cnc));
            Functions.Add(new WritePlcRegisterFunction(cnc));
            Functions.Add(new ReadPlcTypeFunction(cnc));
            Functions.Add(new ReadPlcType2Function(cnc));
            Functions.Add(new ReadPlcAddrFunction(cnc));
            Functions.Add(new ReadPlcVersionFunction(cnc));
            Functions.Add(new DownloadPlcLadderFunction(cnc));
            Functions.Add(new ReadDebugVariableFunction(cnc));
            Functions.Add(new ReadSystemVariableFunction(cnc));
            Functions.Add(new ReadOffsetTitleFunction(cnc));
            Functions.Add(new ReadOffsetCountFunction(cnc));
            Functions.Add(new ReadOffsetAllFunction(cnc));
            Functions.Add(new ReadOffsetScopeFunction(cnc));
            Functions.Add(new ReadOffsetSingleFunction(cnc));
            Functions.Add(new WriteOffsetAllFunction(cnc));
            Functions.Add(new WriteOffsetSingleFunction(cnc));
            Functions.Add(new ReadNcOpLogFunction(cnc));
            Functions.Add(new WriteNcMainFunction(cnc));
            Functions.Add(new ReadNcPointerFunction(cnc));
            Functions.Add(new ReadNcCurrentBlockFunction(cnc));
            Functions.Add(new ReadNcFreeSpaceFunction(cnc));
            Functions.Add(new ReadNcMemListFunction(cnc));
        }

       
    }
}