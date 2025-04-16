using ReactiveUI;
using Syntec.Remote;
using System;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Syntec.TestApp.WPF.ViewModels;
using System.Reactive;
using System.IO;
using System.Linq;
using ControlzEx.Standard;

public abstract class RemoteFunctionModel : ReactiveCncModel
{
    // Реактивные свойства для UI
    private string _name;
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private string _description;
    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    private string _signature;
    public string Signature
    {
        get => _signature;
        set => this.RaiseAndSetIfChanged(ref _signature, value);
    }

    private object _result;
    public object Result
    {
        get => _result;
        set => this.RaiseAndSetIfChanged(ref _result, value);
    }

    // Коллекции параметров
    public ObservableCollection<Parameter> InputParameters { get; } = new ObservableCollection<Parameter>();
    public ObservableCollection<Parameter> OutputParameters { get; } = new ObservableCollection<Parameter>();

    protected RemoteFunctionModel(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        // Подписка на изменения коллекции
        InputParameters.CollectionChanged += (sender, args) =>
            this.RaisePropertyChanged(nameof(InputParameters));

        // Подписка на изменения коллекции
        OutputParameters.CollectionChanged += (sender, args) =>
            this.RaisePropertyChanged(nameof(InputParameters));

        ExecuteCommand = ReactiveCommand.Create(Execute);
    }

    // Команда для остановки опроса 
    public ReactiveCommand<Unit, Unit> ExecuteCommand { get; }

    protected object DefaultResult(short result)
    {
        return new  
        { 
            ResultCode = result, 
            ResultDescription = GetErrorDescription(result), 
            InputData = InputParameters, 
            OutputData = OutputParameters 
        };
    }

    protected object ExceptionResult(Exception ex)
    {
        return new
        {
            ResultCode = ex.HResult,
            ResultDescription = ex.Message,
            InputData = InputParameters,
            OutputData = OutputParameters
        };
    }

    // Метод выполнения функции (реализуется в наследниках)
    public abstract void Execute();

    public static bool IsDebug = true;

    protected virtual bool CanExecute()
    {
        OnNotification($"Запускаем функцию {Name} : {Description}", NotificationType.Debug);

        if (IsDebug)
            return true;

        if (RemoteCnc == null || !RemoteCnc.isConnected())
        {
            Result = "Ошибка: ЧПУ не подключено";
            OnNotification((string)Result);

            return false;
        }

        return true;
    }

    // Форматирование результата
    public virtual string FormatResult(object result)
    {
        return JsonConvert.SerializeObject(result, Formatting.Indented);
    }

    public override void UpdateInternalState()
    {

    }

    protected void HandleResult(short result, Action onSuccess, string successMessage = "Операция выполнена успешно")
    {
        Result = DefaultResult(result);

        if (result == 0)
        {
            onSuccess?.Invoke();
            OnNotification(successMessage, NotificationType.Info);
        }
        else
        {
            string errorMessage = GetErrorDescription(result);
            OnNotification($"Ошибка [{result}] : {errorMessage}", NotificationType.Error);
        }
    }

    public static string GetErrorDescription(short errorCode)
    {
        if (errorCode == 0)
            return "Успешное выполнение";
        
        var error = (SyntecRemoteCNC.ErrorCode)errorCode;
        switch (error)
        {
            case SyntecRemoteCNC.ErrorCode.NotSupported:
                return "Функция не поддерживается";
            case SyntecRemoteCNC.ErrorCode.ProtocolError:
                return "Ошибка протокола связи";
            case SyntecRemoteCNC.ErrorCode.SocketError:
                return "Ошибка сокета (проблемы с подключением)";
            case SyntecRemoteCNC.ErrorCode.DLLFileError:
                return "Ошибка загрузки DLL";
            case SyntecRemoteCNC.ErrorCode.USBEmpty:
                return "USB-устройство не содержит данных";
            case SyntecRemoteCNC.ErrorCode.NoUSB:
                return "USB-устройство не обнаружено";
            case SyntecRemoteCNC.ErrorCode.HandleNumberError:
                return "Ошибка номера дескриптора";
            case SyntecRemoteCNC.ErrorCode.VersionMismach:
                return "Несоответствие версий ПО";
            case SyntecRemoteCNC.ErrorCode.AbnormalLibraryState:
                return "Некорректное состояние библиотеки";
            case SyntecRemoteCNC.ErrorCode.RestOrStopRequest:
                return "Запрос на остановку или перезапуск";
            case SyntecRemoteCNC.ErrorCode.CNCBusy:
                return "ЧПУ занято (выполняется другая операция)";
            case SyntecRemoteCNC.ErrorCode.NormalTermination:
                return "Нормальное завершение (не ошибка)";
            case SyntecRemoteCNC.ErrorCode.ErrorFunctionNotExecuted:
                return "Функция не выполнена";
            case SyntecRemoteCNC.ErrorCode.ErrorDataBlockLength:
                return "Ошибка длины блока данных";
            case SyntecRemoteCNC.ErrorCode.ErrorDataNumber:
                return "Ошибка номера данных";
            case SyntecRemoteCNC.ErrorCode.ErrorDataAttribute:
                return "Ошибка атрибута данных";
            case SyntecRemoteCNC.ErrorCode.ErrorData:
                return "Ошибка данных (некорректные или поврежденные)";
            case SyntecRemoteCNC.ErrorCode.ErrorNoOption:
                return "Опция недоступна";
            case SyntecRemoteCNC.ErrorCode.ErrorWriteProtection:
                return "Ошибка записи (защита от записи)";
            case SyntecRemoteCNC.ErrorCode.ErrorMemoryOverflow:
                return "Переполнение памяти";
            case SyntecRemoteCNC.ErrorCode.ErrorCNCParameter:
                return "Ошибка параметра ЧПУ";
            case SyntecRemoteCNC.ErrorCode.ErrorBufferEmptyOrFull:
                return "Буфер пуст или переполнен";
            case SyntecRemoteCNC.ErrorCode.ErrorPathNumber:
                return "Ошибка номера пути";
            case SyntecRemoteCNC.ErrorCode.ErrorCNCMode:
                return "Некорректный режим ЧПУ";
            case SyntecRemoteCNC.ErrorCode.ErrorCNCExecutionReject:
                return "ЧПУ отклонило выполнение команды";
            case SyntecRemoteCNC.ErrorCode.ErrorDataServer:
                return "Ошибка сервера данных";
            case SyntecRemoteCNC.ErrorCode.ErrorAlarm:
                return "Аварийная ситуация";
            case SyntecRemoteCNC.ErrorCode.ErrorStop:
                return "Остановка выполнения";
            case SyntecRemoteCNC.ErrorCode.ErrorDataProtection:
                return "Ошибка защиты данных";
            case SyntecRemoteCNC.ErrorCode.ErrorNotFoundMachineID:
                return "Идентификатор станка не найден";
            case SyntecRemoteCNC.ErrorCode.ErrorNoOut:
                return "Нет выходных данных";
            default:
                return $"Неизвестная ошибка (код: {errorCode})";
        }
    }

    /// <summary>
    /// Теги функций для фильтрации
    /// </summary>
    public ObservableCollection<string> Tags { get; } = new ObservableCollection<string>();

    /// <summary>
    /// Добавление списка тегов
    /// </summary>
    /// <param name="tags">Список тегов для фильтрации</param>
    protected void AddTags(params string[] tags)
    {
        foreach (var tag in tags)
        {
            if (!Tags.Contains(tag))
                Tags.Add(tag);
        }
    }
}

//------------------------------------------------------------------------------------------------------
//short READ_useTime(out string Status, out string TimeStart, out string TimeExpire, out int TimeRemain)
//------------------------------------------------------------------------------------------------------
public class ReadUseTime: RemoteFunctionModel
{
    public ReadUseTime(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name        = "READ_useTime";
        Description = "Получение времени использования ЧПУ";
        Signature   = "short READ_useTime(out string Status, out string TimeStart, out string TimeExpire, out int TimeRemain)";

        //Добавление тегов
        AddTags("Чтение", "Система", "Время");

        #region Инициализация параметров
        OutputParameters.Add(new Parameter 
        {   
            Name = "Status",  Type = typeof(string), Description = "Текстовое представление текущего состояния" 
        });

        OutputParameters.Add(new Parameter 
        { 
            Name = "TimeStart", Type = typeof(string), Description = "Время старта" });

        OutputParameters.Add(new Parameter
        {
            Name = "TimeExpire", Type = typeof(string), Description = "Время истечения"
        });

        OutputParameters.Add(new Parameter 
        { 
            Name = "TimeRemain", Type = typeof(int), Description = "Оставшееся время" 
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;
            
        try
        {
            string status, timeStart, timeExpire;
            int timeRemain;

            short result = RemoteCnc.READ_useTime(
                out status,
                out timeStart,
                out timeExpire,
                out timeRemain
            );

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = status;
                OutputParameters[1].Value = timeStart;
                OutputParameters[2].Value = timeExpire;
                OutputParameters[3].Value = timeRemain;
                
            }, "Данные времени использования успешно получены");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
//short READ_information(out short Axes, out string CncType, out short MaxAxes, out string Series, out string Nc_Ver, out string[] AxisName)
//------------------------------------------------------------------------------------------------------
public class ReadInformation : RemoteFunctionModel
{
    public ReadInformation(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_information";
        Description = "Получение информации о конфигурации ЧПУ";
        Signature = "short READ_information(out short Axes, out string CncType, out short MaxAxes, out string Series, out string Nc_Ver, out string[] AxisName)";

        //Добавление тегов
        AddTags("Чтение", "Система", "Информация");

        #region Инициализация параметров
        OutputParameters.Add(new Parameter
        {
            Name = "Axes", Type = typeof(short), Description = "Текущее количество активных осей"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "CncType", Type = typeof(string), Description = "Тип системы ЧПУ"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "MaxAxes", Type = typeof(short), Description = "Максимальное поддерживаемое количество осей"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "Series", Type = typeof(string), Description = "Серия оборудования"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "Nc_Ver", Type = typeof(string), Description = "Версия ПО ЧПУ"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "AxisName", Type = typeof(string[]), Description = "Массив имен осей"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            short axes, maxAxes;
            string cncType, series, ncVer;
            string[] axisNames;

            short result = RemoteCnc.READ_information(
                out axes,
                out cncType,
                out maxAxes,
                out series,
                out ncVer,
                out axisNames
            );
            HandleResult(result, () =>
            {
                OutputParameters[0].Value = axes;
                OutputParameters[1].Value = cncType;
                OutputParameters[2].Value = maxAxes;
                OutputParameters[3].Value = series;
                OutputParameters[4].Value = ncVer;
                OutputParameters[5].Value = axisNames;
                
            }, "Информация о ЧПУ получена");   
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
//short READ_status(out string MainProg, out string CurProg, out int CurSeq, out string Mode, out string Status, out string Alarm, out string EMG)
//------------------------------------------------------------------------------------------------------
public class ReadStatusFunction : RemoteFunctionModel
{
    public ReadStatusFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name        = "READ_status";
        Description = "Получение текущего статуса ЧПУ";
        Signature = "short READ_status(out string MainProg, out string CurProg, out int CurSeq, " +
                    "out string Mode, out string Status, out string Alarm, out string EMG)";

        //Добавление тегов
        AddTags("Чтение", "Система", "Состояние", "Авария");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "MainProg", Type = typeof(string), Description = "Имя главной программы"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "CurProg", Type = typeof(string), Description = "Имя текущей программы"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "CurSeq", Type = typeof(int), Description = "Текущий номер последовательности"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "Mode", Type = typeof(string), Description = "Режим работы ЧПУ"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "Status", Type = typeof(string), Description = "Текущий статус системы"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "Alarm", Type = typeof(string), Description = "Текущая авария"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "EMG", Type = typeof(string), Description = "Состояние аварийной остановки"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string mainProg, curProg, mode, status, alarm, emg;
            int curSeq;

            short result = RemoteCnc.READ_status(
                out mainProg,
                out curProg,
                out curSeq,
                out mode,
                out status,
                out alarm,
                out emg
            );

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = mainProg;
                OutputParameters[1].Value = curProg;
                OutputParameters[2].Value = curSeq;
                OutputParameters[3].Value = mode;
                OutputParameters[4].Value = status;
                OutputParameters[5].Value = alarm;
                OutputParameters[6].Value = emg;
                
            }, "Данные текущем состоянии получены");
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
//short READ_position(out string[] AxisName, out short DecPoint, out string[] Unit, out float[] Mach, out float[] Abs, out float[] Rel, out float[] Dist)
//------------------------------------------------------------------------------------------------------
public class ReadPositionFunction : RemoteFunctionModel
{
    public ReadPositionFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_position";
        Description = "Получение текущих позиций осей ЧПУ";
        Signature = "short READ_position(out string[] AxisName, out short DecPoint, out string[] Unit, " +
                    "out float[] Mach, out float[] Abs, out float[] Rel, out float[] Dist)";

        //Добавление тегов
        AddTags("Чтение", "Ось", "Положение");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "AxisName", Type = typeof(string[]), Description = "Массив имен осей"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "DecPoint", Type = typeof(short), Description = "Количество знаков после запятой"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "Unit", Type = typeof(string[]), Description = "Единицы измерения для каждой оси"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "Mach", Type = typeof(float[]), Description = "Машинные координаты"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "Abs", Type = typeof(float[]), Description = "Абсолютные координаты"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "Rel", Type = typeof(float[]), Description = "Относительные координаты"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "Dist", Type = typeof(float[]), Description = "Дистанции до цели"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string[] axisNames;
            short decPoint;
            string[] units;
            float[] machPos, absPos, relPos, distPos;

            short result = RemoteCnc.READ_position(
                out axisNames,
                out decPoint,
                out units,
                out machPos,
                out absPos,
                out relPos,
                out distPos
            );            
            
            HandleResult(result, () =>
            {
                OutputParameters[0].Value = axisNames;  
                OutputParameters[1].Value = decPoint;   
                OutputParameters[2].Value = units;      
                OutputParameters[3].Value = machPos;    
                OutputParameters[4].Value = absPos;     
                OutputParameters[5].Value = relPos;     
                OutputParameters[6].Value = distPos;    
                
            }, "Данные о текущих координатах получены");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
//short WRITE_relpos(string AxisName, double PosValue)
//------------------------------------------------------------------------------------------------------
public class WriteRelPosFunction : RemoteFunctionModel
{
    public WriteRelPosFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "WRITE_relpos";
        Description = "Запись относительной позиции для указанной оси";
        Signature = "short WRITE_relpos(string AxisName, double PosValue)";

        //Добавление тегов
        AddTags("Запись", "Ось", "Положение");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "AxisName", Type = typeof(string), Description = "Имя оси (например, X, Y, Z)", Value = "X" 
        });

        InputParameters.Add(new Parameter
        {
            Name = "PosValue", Type = typeof(double), Description = "Значение позиции для установки", Value = 0.0 
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            // Получаем значения входных параметров
            string axisName = InputParameters[0].Value?.ToString() ?? "X";
            double posValue = Convert.ToDouble(InputParameters[1].Value);

            // Вызываем метод ЧПУ
            short result = RemoteCnc.WRITE_relpos(axisName, posValue);

            HandleResult(result, () =>
            {
                
            }, $"Записана относительная позиция {posValue} для оси {axisName}");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
//short READ_gcode(out string[] GData)
//------------------------------------------------------------------------------------------------------
public class ReadGCodeFunction : RemoteFunctionModel
{
    public ReadGCodeFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_gcode";
        Description = "Чтение текущих G-кодов ЧПУ";
        Signature = "short READ_gcode(out string[] GData)";

        //Добавление тегов
        AddTags("Чтение", "Коды");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "GData", Type = typeof(string[]), Description = "Массив активных G-кодов"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string[] gCodes;
            short result = RemoteCnc.READ_gcode(out gCodes);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = gCodes;
                
            }, $"Получены G-коды");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
//short READ_othercode(out int HCode, out int DCode, out int TCode, out int MCode, out int FCode, out int SCode)
//------------------------------------------------------------------------------------------------------
public class ReadOtherCodesFunction : RemoteFunctionModel
{
    public ReadOtherCodesFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_othercode";
        Description = "Чтение дополнительных кодов ЧПУ (H, D, T, M, F, S)";
        Signature = "short READ_othercode(out int HCode, out int DCode, out int TCode, " +
                    "out int MCode, out int FCode, out int SCode)";

        //Добавление тегов
        AddTags("Чтение", "Коды");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "HCode", Type = typeof(int), Description = "Текущий H-код (компенсация длины инструмента)"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "DCode", Type = typeof(int), Description = "Текущий D-код (компенсация радиуса инструмента)"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "TCode", Type = typeof(int), Description = "Текущий T-код (номер инструмента)"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "MCode", Type = typeof(int), Description = "Текущий M-код (вспомогательная функция)"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "FCode", Type = typeof(int), Description = "Текущий F-код (скорость подачи)"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "SCode", Type = typeof(int), Description = "Текущий S-код (скорость шпинделя)"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            int hCode, dCode, tCode, mCode, fCode, sCode;
            short result = RemoteCnc.READ_othercode(
                out hCode,
                out dCode,
                out tCode,
                out mCode,
                out fCode,
                out sCode
            );

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = hCode; 
                OutputParameters[1].Value = dCode; 
                OutputParameters[2].Value = tCode; 
                OutputParameters[3].Value = mCode; 
                OutputParameters[4].Value = fCode; 
                OutputParameters[5].Value = sCode; 
                
            }, $"Получены дополнительные коды");            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
//short short READ_spindle(out float OvFeed, out float OvSpindle, out float ActFeed, out int ActSpindle)
//------------------------------------------------------------------------------------------------------
public class ReadSpindleFunction : RemoteFunctionModel
{
    public ReadSpindleFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_spindle";
        Description = "Чтение параметров шпинделя и подачи";
        Signature = "short READ_spindle(out float OvFeed, out float OvSpindle, out float ActFeed, out int ActSpindle)";

        //Добавление тегов
        AddTags("Чтение", "Шпиндель", "Положение");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "OvFeed", Type = typeof(float), Description = "Коэффициент коррекции подачи (%)"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "OvSpindle", Type = typeof(float), Description = "Коэффициент коррекции скорости шпинделя (%)"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "ActFeed", Type = typeof(float), Description = "Фактическая скорость подачи (мм/мин)"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "ActSpindle", Type = typeof(int), Description = "Фактическая скорость шпинделя (об/мин)"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            float ovFeed, ovSpindle, actFeed;
            int actSpindle;

            short result = RemoteCnc.READ_spindle(
                out ovFeed,
                out ovSpindle,
                out actFeed,
                out actSpindle
            );

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = ovFeed;        
                OutputParameters[1].Value = ovSpindle;     
                OutputParameters[2].Value = actFeed;       
                OutputParameters[3].Value = actSpindle;    
                
            }, $"Получены данные о шпинделе");

            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
//short READ_time(out int PowerOnTime, out int AccumulateCuttingTime, out int CuttingTimePerCycle, out int WorkTime)
//------------------------------------------------------------------------------------------------------
public class ReadMachineTimeFunction : RemoteFunctionModel
{
    public ReadMachineTimeFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_time";
        Description = "Чтение времени работы станка";
        Signature = "short READ_time(out int PowerOnTime, out int AccumulateCuttingTime, " +
                    "out int CuttingTimePerCycle, out int WorkTime)";

        //Добавление тегов
        AddTags("Чтение", "Время", "Система");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "PowerOnTime", Type = typeof(int), Description = "Общее время включения (минуты)"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "AccumulateCuttingTime", Type = typeof(int), Description = "Накопленное время резания (минуты)"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "CuttingTimePerCycle", Type = typeof(int), Description = "Время резания текущего цикла (минуты)"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "WorkTime", Type = typeof(int), Description = "Общее рабочее время (минуты)"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            int powerOnTime, accumulateCuttingTime, cuttingTimePerCycle, workTime;

            short result = RemoteCnc.READ_time(
                out powerOnTime,
                out accumulateCuttingTime,
                out cuttingTimePerCycle,
                out workTime
            );

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = powerOnTime;             
                OutputParameters[1].Value = accumulateCuttingTime;   
                OutputParameters[2].Value = cuttingTimePerCycle;   
                OutputParameters[3].Value = workTime;                
                
            }, $"Получены данные о времени работы станка");
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
//short READ_part_count(out int part_count, out int require_part_count, out int total_part_count)
//------------------------------------------------------------------------------------------------------
public class ReadPartCountFunction : RemoteFunctionModel
{
    public ReadPartCountFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_part_count";
        Description = "Чтение счетчика деталей";
        Signature = "short READ_part_count(out int part_count, out int require_part_count, out int total_part_count)";

        //Добавление тегов
        AddTags("Чтение", "Счетчик", "Система");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "CurrentCount", Type = typeof(int), Description = "Количество изготовленных деталей в текущей серии"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "RequiredCount", Type = typeof(int), Description = "Плановое количество деталей"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "TotalCount", Type = typeof(int), Description = "Общее количество изготовленных деталей"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            int partCount, requirePartCount, totalPartCount;

            short result = RemoteCnc.READ_part_count(
                out partCount,
                out requirePartCount,
                out totalPartCount
            );

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = partCount;          
                OutputParameters[1].Value = requirePartCount;   
                OutputParameters[2].Value = totalPartCount;     
                
            }, $"Получены данные от счетчика деталей");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
//short DOWNLOAD_work_record(string Destination)
//------------------------------------------------------------------------------------------------------
public class DownloadWorkRecordFunction : RemoteFunctionModel
{
    public DownloadWorkRecordFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "DOWNLOAD_work_record";
        Description = "Скачивание файла рабочих записей станка";
        Signature = "short DOWNLOAD_work_record(string Destination)";

        //Добавление тегов
        AddTags("Скачать", "Система", "Файл");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "DestinationPath", Type = typeof(string), Description = "Локальный путь для сохранения файла",
            Value = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string destinationPath = InputParameters[0].Value?.ToString();

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new ArgumentException("Не указан путь для сохранения");
            }

            // Добавляем имя файла, если указана только папка
            if (Directory.Exists(destinationPath) && !Path.HasExtension(destinationPath))
            {
                destinationPath = Path.Combine(destinationPath, $"work_record_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            }

            short result = RemoteCnc.DOWNLOAD_work_record(destinationPath);

            HandleResult(result, () => {}, $"Файл рабочих записей сохранен : {destinationPath}");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
//short READ_alm_current(out bool IsAlarm, out string[] AlmMsg, out DateTime[] AlmTime)
//------------------------------------------------------------------------------------------------------
public class ReadCurrentAlarmFunction : RemoteFunctionModel
{
    public ReadCurrentAlarmFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_alm_current";
        Description = "Чтение текущих аварийных сообщений";
        Signature = "short READ_alm_current(out bool IsAlarm, out string[] AlmMsg, out DateTime[] AlmTime)";

        //Добавление тегов
        AddTags("Чтение", "Система", "Авария");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "IsAlarm", Type = typeof(bool), Description = "Флаг наличия активных аварий"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "AlmMsg", Type = typeof(string[]), Description = "Массив аварийных сообщений"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "AlmTime", Type = typeof(DateTime[]), Description = "Массив времен возникновения аварий"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            bool isAlarm;
            string[] almMsg;
            DateTime[] almTime;

            short result = RemoteCnc.READ_alm_current(
                out isAlarm,
                out almMsg,
                out almTime
            );

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = isAlarm;    
                OutputParameters[1].Value = almMsg;     
                OutputParameters[2].Value = almTime; 
                
            }, $"Получены текущие аварийные сообщения");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
//short READ_alm_history(out string[] AlmMsg, out DateTime[] AlmTime)
//------------------------------------------------------------------------------------------------------
public class ReadAlarmHistoryFunction : RemoteFunctionModel
{
    public ReadAlarmHistoryFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_alm_history";
        Description = "Чтение истории аварийных сообщений";
        Signature = "short READ_alm_history(out string[] AlmMsg, out DateTime[] AlmTime)";

        //Добавление тегов
        AddTags("Чтение", "Система", "Авария");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "AlmMsg", Type = typeof(string[]), Description = "Массив исторических аварийных сообщений"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "AlmTime", Type = typeof(DateTime[]), Description = "Массив времен возникновения аварий"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string[] almMsg;
            DateTime[] almTime;

            short result = RemoteCnc.READ_alm_history(
                out almMsg,
                out almTime
            );

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = almMsg ?? Array.Empty<string>();
                OutputParameters[1].Value = almTime ?? Array.Empty<DateTime>();
                
            }, $"Получены исторические аварийные сообщения");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
//short short READ_work_coord_axis(out string[] WorkCoordTitle)
//------------------------------------------------------------------------------------------------------
public class ReadWorkCoordAxisFunction : RemoteFunctionModel
{
    public ReadWorkCoordAxisFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_work_coord_axis";
        Description = "Чтение наименований рабочих координатных осей";
        Signature = "short READ_work_coord_axis(out string[] WorkCoordTitle)";

        //Добавление тегов
        AddTags("Чтение", "Ось");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "WorkCoordTitle", Type = typeof(string[]), Description = "Массив наименований рабочих координатных осей"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string[] axisTitles;
            short result = RemoteCnc.READ_work_coord_axis(out axisTitles);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = axisTitles ?? Array.Empty<string>();
                
            }, $"Получены наименования рабочих координат осей");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
//short READ_work_coord_scope(int StartNumber, int EndNumber, out string[] CoordName, out float[][] WorkCoord)
//------------------------------------------------------------------------------------------------------
public class ReadWorkCoordinatesScopeFunction : RemoteFunctionModel
{
    public ReadWorkCoordinatesScopeFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_work_coord_scope";
        Description = "Чтение рабочих координат в указанном диапазоне";
        Signature = "short READ_work_coord_scope(int StartNumber, int EndNumber, out string[] CoordName, out float[][] WorkCoord)";

        //Добавление тегов
        AddTags("Чтение", "Ось", "Диапазон", "Координаты");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "StartNumber", Type = typeof(int), Description = "Начальный номер системы координат", Value = 1 // G54 обычно соответствует 1
        });

        InputParameters.Add(new Parameter
        {
            Name = "EndNumber", Type = typeof(int), Description = "Конечный номер системы координат", Value = 6 // До G59
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "CoordName", Type = typeof(string[]), Description = "Массив имен систем координат"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "WorkCoord", Type = typeof(float[][]), Description = "Массив массивов значений координат"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            // Получаем входные параметры
            int start = (int)InputParameters[0].Value;
            int end = (int)InputParameters[1].Value;

            // Проверка допустимости диапазона
            if (start < 1 || end < start)
            {
                throw new ArgumentException("Ошибка: Некорректный диапазон систем координат");
            }

            string[] coordNames;
            float[][] workCoords;

            // Вызов метода ЧПУ
            short result = RemoteCnc.READ_work_coord_scope(
                start,
                end,
                out coordNames,
                out workCoords
            );

            HandleResult(result, () =>
            {               
                OutputParameters[0].Value = coordNames ?? Array.Empty<string>();
                OutputParameters[1].Value = workCoords ?? Array.Empty<float[]>();
                
            }, $"Получены рабочие координаты в заданном диапазоне : [{start}-{end}]");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }        
}


//------------------------------------------------------------------------------------------------------
//short short READ_work_coord_single(string CoordName, out float[] WorkCoord)
//------------------------------------------------------------------------------------------------------
public class ReadWorkCoordinateSingleFunction : RemoteFunctionModel
{
    public ReadWorkCoordinateSingleFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_work_coord_single";
        Description = "Чтение рабочих координат для указанной системы";
        Signature = "short READ_work_coord_single(string CoordName, out float[] WorkCoord)";

        //Добавление тегов
        AddTags("Чтение", "Координаты");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "CoordName", Type = typeof(string), Description = "Имя системы координат (например, G54, G55)",Value = "G54" 
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "WorkCoord", Type = typeof(float[]), Description = "Массив значений координат"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string coordName = InputParameters[0].Value?.ToString();
            if (string.IsNullOrEmpty(coordName))
            {
                throw new ArgumentNullException("Не указано имя системы координат");
            }

            float[] workCoord;
            short result = RemoteCnc.READ_work_coord_single(
                coordName,
                out workCoord
            );

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = workCoord ?? Array.Empty<float>();
                
            }, $"Получены рабочие координаты для  [{coordName}]");            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
//short WRITE_work_coord_all(string[] CoordName, float[][] WorkCoord)
//------------------------------------------------------------------------------------------------------
public class WriteWorkCoordinatesAllFunction : RemoteFunctionModel
{
    public WriteWorkCoordinatesAllFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "WRITE_work_coord_all";
        Description = "Запись рабочих координат для нескольких систем";
        Signature = "short WRITE_work_coord_all(string[] CoordName, float[][] WorkCoord)";

        //Добавление тегов
        AddTags("Запись", "Координаты");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "CoordName", Type = typeof(string[]), Description = "Массив имен систем координат (G54, G55 и т.д.)", Value = new string[] { "G54" } 
        });

        InputParameters.Add(new Parameter
        {
            Name = "WorkCoord", Type = typeof(float[][]), Description = "Массив массивов значений координат", Value = new float[][] { new float[] { 0f, 0f, 0f } } 
        });
        #endregion

    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string[] coordNames  = InputParameters[0].Value as string[] ?? new string[0];
            float[][] workCoords = InputParameters[1].Value as float[][] ?? new float[0][];

            // Проверка согласованности данных
            if (coordNames.Length != workCoords.Length)
            {
                throw new ArgumentException("Количество систем не соответствует количеству наборов координат");
            }

            // Проверка пустых данных
            if (coordNames.Length == 0 || workCoords.Length == 0)
            {
                throw new ArgumentException("Нет данных для записи");
            }

            short result = RemoteCnc.WRITE_work_coord_all(
                coordNames,
                workCoords
            );

            string formattedResult = string.Join(", ",
                                    coordNames.Zip(workCoords, (name, coords) =>
                                    $"[{(name ?? "Unknown")}: {(coords != null ? string.Join(", ", coords) : "N/A")}]" ));

            HandleResult(result, () => { }, $"Координаты для рабочих систем были записаны  [{formattedResult}]");
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
//short WRITE_work_coord_single(string CoordName, float[] WorkCoord)
//------------------------------------------------------------------------------------------------------
public class WriteWorkCoordinateSingleFunction : RemoteFunctionModel
{
    public WriteWorkCoordinateSingleFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "WRITE_work_coord_single";
        Description = "Запись рабочих координат для указанной системы";
        Signature = "short WRITE_work_coord_single(string CoordName, float[] WorkCoord)";

        //Добавление тегов
        AddTags("Запись", "Координаты");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "CoordName", Type = typeof(string), Description = "Имя системы координат (например, G54, G55)", Value = "G54" 
        });

        InputParameters.Add(new Parameter
        {
            Name = "WorkCoord", Type = typeof(float[]), Description = "Массив значений координат [X,Y,Z,...]", Value = new float[] { 0f, 0f, 0f } 
        });
        #endregion

    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string coordName = InputParameters[0].Value?.ToString();
            float[] workCoord = InputParameters[1].Value as float[];

            // Валидация входных данных
            if (string.IsNullOrEmpty(coordName))
            {
                throw new ArgumentException("Не указано имя системы координат");
            }

            if (workCoord == null || workCoord.Length == 0)
            {
                throw new ArgumentException("Не указаны координаты для записи");
            }

            short result = RemoteCnc.WRITE_work_coord_single(
                coordName,
                workCoord
            );

            string formattedResult = string.Join(", ", workCoord);

            HandleResult(result, () => { }, $"Координаты для рабочей системы {coordName} были записаны  [{formattedResult}]");

        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
//short READ_work_coord_count(out short Count)
//------------------------------------------------------------------------------------------------------
public class ReadWorkCoordCountFunction : RemoteFunctionModel
{
    public ReadWorkCoordCountFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_work_coord_count";
        Description = "Чтение количества доступных систем рабочих координат";
        Signature = "short READ_work_coord_count(out short Count)";

        //Добавление тегов
        AddTags("Чтение", "Координаты");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "Count", Type = typeof(short), Description = "Количество доступных систем рабочих координат"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            short count;
            short result = RemoteCnc.READ_work_coord_count(out count);


            HandleResult(result, () =>
            {
                OutputParameters[0].Value = count;
                
            }, $"Получено количество доступных систем рабочих координат");

        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }        
}


//------------------------------------------------------------------------------------------------------
//short READ_macro_all(out int[] MacroNumber, out double[] MacroData)
//------------------------------------------------------------------------------------------------------
public class ReadMacroAllFunction : RemoteFunctionModel
{
    public ReadMacroAllFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_macro_all";
        Description = "Чтение всех переменных макропрограммирования";
        Signature = "short READ_macro_all(out int[] MacroNumber, out double[] MacroData)";


        //Добавление тегов
        AddTags("Чтение", "Макро");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "MacroNumber", Type = typeof(int[]), Description = "Массив номеров макропеременных"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "MacroValues", Type = typeof(double[]), Description = "Массив значений макропеременных"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            int[] macroNumbers;
            double[] macroData;

            short result = RemoteCnc.READ_macro_all(
                out macroNumbers,
                out macroData
            );

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = macroNumbers ?? Array.Empty<int>(); 
                OutputParameters[1].Value = macroData ?? Array.Empty<double>();
                
            }, $"Получены переменные макропрограммирования");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }     
}


//------------------------------------------------------------------------------------------------------
//short READ_macro_scope(int StartNumber, int EndNumber, out int[] MacroNumber, out double[] MacroData)
//------------------------------------------------------------------------------------------------------
public class ReadMacroScopeFunction : RemoteFunctionModel
{
    public ReadMacroScopeFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_macro_scope";
        Description = "Чтение переменных макропрограммирования в указанном диапазоне";
        Signature = "short READ_macro_scope(int StartNumber, int EndNumber, out int[] MacroNumber, out double[] MacroData)";

        //Добавление тегов
        AddTags("Чтение", "Макро");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "StartNumber", Type = typeof(int), Description = "Начальный номер макропеременной", Value = 1 // Начальное значение по умолчанию
        });

        InputParameters.Add(new Parameter
        {
            Name = "EndNumber", Type = typeof(int), Description = "Конечный номер макропеременной", Value = 10 // Конечное значение по умолчанию
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "MacroNumber", Type = typeof(int[]), Description = "Массив номеров макропеременных"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "MacroData", Type = typeof(double[]), Description = "Массив значений макропеременных"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            // Получаем входные параметры
            int start   = (int)InputParameters[0].Value;
            int end     = (int)InputParameters[1].Value;

            // Проверка валидности диапазона
            if (start < 1 || end < start)
            {
                throw new ArgumentException("Некорректный диапазон переменных");
            }

            int[] macroNumbers;
            double[] macroData;

            // Вызов метода ЧПУ
            short result = RemoteCnc.READ_macro_scope(
                start,
                end,
                out macroNumbers,
                out macroData
            );

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = macroNumbers ?? Array.Empty<int>();
                OutputParameters[1].Value = macroData ?? Array.Empty<double>();
                
            }, $"Получены переменные макропрограммирования в заданном диапазоне [{start}-{end}]");

            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
//short READ_macro_single(int MacroNumber, out double MacroData)
//------------------------------------------------------------------------------------------------------
public class ReadMacroSingleFunction : RemoteFunctionModel
{
    public ReadMacroSingleFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_macro_single";
        Description = "Чтение одиночной переменной макропрограммирования";
        Signature = "short READ_macro_single(int MacroNumber, out double MacroData)";

        //Добавление тегов
        AddTags("Чтение", "Макро");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "MacroNumber", Type = typeof(int), Description = "Номер макропеременной", Value = 1 
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "MacroData", Type = typeof(double), Description = "Значение макропеременной"
        });

        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            // Получаем номер макропеременной из входных параметров
            int macroNumber = (int)InputParameters[0].Value;
            double macroData;

            // Вызываем метод ЧПУ
            short result = RemoteCnc.READ_macro_single(
                macroNumber,
                out macroData
            );

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = macroData;     
                OutputParameters[1].Value = result;        
                
            }, $"Получена макропеременная [{macroNumber}:{macroData}]");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
// short READ_macro_variable(out int[][] Variable)
//------------------------------------------------------------------------------------------------------
public class ReadMacroVariableFunction : RemoteFunctionModel
{
    public ReadMacroVariableFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_macro_variable";
        Description = "Чтение многомерных макропеременных";
        Signature = "short READ_macro_variable(out int[][] Variable)";

        //Добавление тегов
        AddTags("Чтение", "Макро");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "Variables", Type = typeof(int[][]), Description = "Массив массивов значений макропеременных"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            int[][] variables;
            short result = RemoteCnc.READ_macro_variable(out variables);


            HandleResult(result, () =>
            {
                OutputParameters[0].Value = variables ?? Array.Empty<int[]>();
                
            }, $"Получены массивы макропеременных");

        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }    
}

//------------------------------------------------------------------------------------------------------
// short WRITE_macro_all(int[] MacroNumber, double[] MacroData)
//------------------------------------------------------------------------------------------------------
public class WriteMacroAllFunction : RemoteFunctionModel
{
    public WriteMacroAllFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "WRITE_macro_all";
        Description = "Запись значений для нескольких макропеременных";
        Signature = "short WRITE_macro_all(int[] MacroNumber, double[] MacroData)";

        //Добавление тегов
        AddTags("Запись", "Макро");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "MacroNumber", Type = typeof(int[]), Description = "Массив номеров макропеременных", Value = new int[] { 1, 2, 3 } // Значения по умолчанию
        });

        InputParameters.Add(new Parameter
        {
            Name = "MacroData", Type = typeof(double[]), Description = "Массив значений для записи", Value = new double[] { 0.0, 0.0, 0.0 } // Значения по умолчанию
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            // Получаем входные параметры
            int[] macroNumbers = InputParameters[0].Value as int[] ?? Array.Empty<int>();
            double[] macroValues = InputParameters[1].Value as double[] ?? Array.Empty<double>();

            // Проверка согласованности данных
            if (macroNumbers.Length != macroValues.Length)
            {
                throw new ArgumentException("Количество номеров не соответствует количеству значений");
            }

            if (macroNumbers.Length == 0)
            {
                throw new ArgumentException("Нет данных для записи");
            }

            // Вызов метода ЧПУ
            short result = RemoteCnc.WRITE_macro_all(macroNumbers, macroValues);

            HandleResult(result, () => { }, $"Значения макропеременных обновлены");
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
// short WRITE_macro_single(int MacroNumber, double MacroData)
//------------------------------------------------------------------------------------------------------
public class WriteMacroSingleFunction : RemoteFunctionModel
{
    public WriteMacroSingleFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "WRITE_macro_single";
        Description = "Запись значения одиночной макропеременной";
        Signature = "short WRITE_macro_single(int MacroNumber, double MacroData)";

        //Добавление тегов
        AddTags("Запись", "Макро");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "MacroNumber", Type = typeof(int), Description = "Номер макропеременной", Value = 1 
        });

        InputParameters.Add(new Parameter
        {
            Name = "MacroValue", Type = typeof(double), Description = "Значение для записи", Value = 0.0 
        });
        #endregion        
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            // Получаем входные параметры
            int macroNumber = (int)InputParameters[0].Value;
            double macroValue = (double)InputParameters[1].Value;

            // Вызов метода ЧПУ
            short result = RemoteCnc.WRITE_macro_single(macroNumber, macroValue);

            HandleResult(result, () =>
            {
                
            }, $"Значения макропеременной обновлено");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
// short READ_param_max(out int MaxNumber)
//------------------------------------------------------------------------------------------------------
public class ReadParamMaxFunction : RemoteFunctionModel
{
    public ReadParamMaxFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_param_max";
        Description = "Чтение максимального номера параметра";
        Signature = "short READ_param_max(out int MaxNumber)";

        //Добавление тегов
        AddTags("Чтение", "Система");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "MaxNumber", Type = typeof(int), Description = "Максимальный номер параметра"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            int maxNumber;
            short result = RemoteCnc.READ_param_max(out maxNumber);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = maxNumber;
                
            }, $"Получено максимальное количество параметров");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short READ_param_data(int ParamStart, int ParamEnd, out int[] ParamData)
//------------------------------------------------------------------------------------------------------
public class ReadParamDataFunction : RemoteFunctionModel
{
    public ReadParamDataFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_param_data";
        Description = "Чтение данных параметров в указанном диапазоне";
        Signature = "short READ_param_data(int ParamStart, int ParamEnd, out int[] ParamData)";

        //Добавление тегов
        AddTags("Чтение", "Система", "Параметры");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "ParamStart", Type = typeof(int), Description = "Начальный номер параметра", Value = 1 
        });

        InputParameters.Add(new Parameter
        {
            Name = "ParamEnd", Type = typeof(int), Description = "Конечный номер параметра", Value = 10 
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "ParamData", Type = typeof(int[]), Description = "Массив значений параметров"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            // Получаем входные параметры
            int paramStart = (int)InputParameters[0].Value;
            int paramEnd = (int)InputParameters[1].Value;

            // Проверка валидности диапазона
            if (paramStart < 1 || paramEnd < paramStart)
            {
                throw new ArgumentException("Некорректный диапазон параметров");
            }

            int[] paramData;
            short result = RemoteCnc.READ_param_data(paramStart, paramEnd, out paramData);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = paramData ?? Array.Empty<int>();
                
            }, $"Получены параметры в заданном диапазоне [{paramStart}-{paramEnd}]");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
// short READ_param_schema(out int[] index, out int[] value, out string[] context, out string[] bound, out int count)
//------------------------------------------------------------------------------------------------------
public class ReadParamSchemaFunction : RemoteFunctionModel
{
    public ReadParamSchemaFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_param_schema";
        Description = "Чтение схемы параметров с индексами, значениями, описаниями и границами";
        Signature = "short READ_param_schema(out int[] index, out int[] value, out string[] context, out string[] bound, out int count)";

        //Добавление тегов
        AddTags("Чтение", "Система", "Параметры");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "index", Type = typeof(int[]), Description = "Массив индексов параметров"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "value", Type = typeof(int[]), Description = "Массив значений параметров"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "context", Type = typeof(string[]), Description = "Массив описаний параметров"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "bound", Type = typeof(string[]), Description = "Массив граничных значений параметров"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "count", Type = typeof(int), Description = "Общее количество параметров"
        });
        
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            int[] indexes;
            int[] values;
            string[] contexts;
            string[] bounds;
            int count;

            short result = RemoteCnc.READ_param_schema(
                out indexes,
                out values,
                out contexts,
                out bounds,
                out count
            );

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = indexes ?? Array.Empty<int>();
                OutputParameters[1].Value = values ?? Array.Empty<int>();
                OutputParameters[2].Value = contexts ?? Array.Empty<string>();
                OutputParameters[3].Value = bounds ?? Array.Empty<string>();
                OutputParameters[4].Value = count;
                
            }, $"Получены схемы параметров с индексами, значениями, описаниями и границами");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short WRITE_param_single(int ParamID, int val)
//------------------------------------------------------------------------------------------------------
public class WriteParamSingleFunction : RemoteFunctionModel
{
    public WriteParamSingleFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "WRITE_param_single";
        Description = "Запись значения одиночного параметра";
        Signature = "short WRITE_param_single(int ParamID, int val)";

        //Добавление тегов
        AddTags("Запись", "Система", "Параметры");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "ParamID", Type = typeof(int), Description = "Идентификатор параметра", Value = 1 
        });

        InputParameters.Add(new Parameter
        {
            Name = "val", Type = typeof(int), Description = "Значение параметра для записи", Value = 0 
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            // Получаем входные параметры
            int paramId = (int)InputParameters[0].Value;
            int paramValue = (int)InputParameters[1].Value;

            // Вызов метода ЧПУ
            short result = RemoteCnc.WRITE_param_single(paramId, paramValue);

            HandleResult(result, () =>
            {           
                
            }, $"Записан одиночный параметр [{paramId} : {paramValue}]");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

/// <summary>
/// Абстрактный базовый класс для чтения входных, выходных, внутренних, статусных и аналоговых битов PLC
/// </summary>
public abstract class ReadPlcBitsFunction : RemoteFunctionModel
{
    protected ReadPlcBitsFunction(SyntecRemoteCNC remoteCnc, string functionName, string description) : base(remoteCnc)
    {
        Name = functionName;
        Description = description;
        Signature = $"short {functionName}(int PlcStart, int PlcEnd, out byte[] PlcData)";

        //Добавление тегов
        AddTags("Чтение", "PLC", "Биты");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "PlcStart", Type = typeof(int), Description = "Начальный адрес битов PLC", Value = 0 
        });

        InputParameters.Add(new Parameter
        {
            Name = "PlcEnd", Type = typeof(int), Description = "Конечный адрес битов PLC", Value = 10 
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "PlcData", Type = typeof(byte[]), Description = "Массив значений входных битов PLC"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            // Получаем входные параметры
            int start = (int)InputParameters[0].Value;
            int end = (int)InputParameters[1].Value;

            // Проверка валидности диапазона
            if (start < 0 || end < start)
            {
                throw new ArgumentException("Некорректный диапазон адресов PLC");
            }

            byte[] plcData;
            short result = ReadBits(start, end, out plcData);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = plcData ?? Array.Empty<byte>();
                
            }, $"{Description} с {start} по {end} успешно прочитаны");

        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }

    protected abstract short ReadBits(int PlcStart, int PlcEnd, out byte[] PlcData);

}

//------------------------------------------------------------------------------------------------------
// short READ_plc_ibit(int PlcStart, int PlcEnd, out byte[] PlcData)
//------------------------------------------------------------------------------------------------------
public class ReadPlcIbitFunction : ReadPlcBitsFunction
{
    public ReadPlcIbitFunction(SyntecRemoteCNC remoteCnc)
        : base(remoteCnc, "READ_plc_ibit", "Чтение входных битов PLC")
    {
    }

    protected override short ReadBits(int start, int end, out byte[] data)
    {
        return RemoteCnc.READ_plc_ibit(start, end, out data);
    }
}


//------------------------------------------------------------------------------------------------------
// short READ_plc_obit(int PlcStart, int PlcEnd, out byte[] PlcData)
//------------------------------------------------------------------------------------------------------
public class ReadPlcObitFunction : ReadPlcBitsFunction
{
    public ReadPlcObitFunction(SyntecRemoteCNC remoteCnc)
        : base(remoteCnc, "READ_plc_obit", "Чтение выходных битов PLC")
    {
    }

    protected override short ReadBits(int start, int end, out byte[] data)
    {
        return RemoteCnc.READ_plc_obit(start, end, out data);
    }
}


//------------------------------------------------------------------------------------------------------
// short READ_plc_cbit(int PlcStart, int PlcEnd, out byte[] PlcData)
//------------------------------------------------------------------------------------------------------
public class ReadPlcCbitFunction : ReadPlcBitsFunction
{
    public ReadPlcCbitFunction(SyntecRemoteCNC remoteCnc)
        : base(remoteCnc, "READ_plc_cbit", "Чтение внутренних битов PLC")
    {
    }

    protected override short ReadBits(int start, int end, out byte[] data)
    {
        return RemoteCnc.READ_plc_cbit(start, end, out data);
    }
}

//------------------------------------------------------------------------------------------------------
// short READ_plc_sbit(int PlcStart, int PlcEnd, out byte[] PlcData)
//------------------------------------------------------------------------------------------------------
public class ReadPlcSbitFunction : ReadPlcBitsFunction
{
    public ReadPlcSbitFunction(SyntecRemoteCNC remoteCnc)
        : base(remoteCnc, "READ_plc_sbit", "Чтение статусных битов PLC")
    {
    }

    protected override short ReadBits(int start, int end, out byte[] data)
    {
        return RemoteCnc.READ_plc_sbit(start, end, out data);
    }
}

//------------------------------------------------------------------------------------------------------
// short READ_plc_abit(int PlcStart, int PlcEnd, out byte[] PlcData)
//------------------------------------------------------------------------------------------------------
public class ReadPlcAbitFunction : ReadPlcBitsFunction
{
    public ReadPlcAbitFunction(SyntecRemoteCNC remoteCnc)
        : base(remoteCnc, "READ_plc_abit", "ЧтениExecutePlcReadе аналоговых битов PLC")
    {
    }

    protected override short ReadBits(int start, int end, out byte[] data)
    {
        return RemoteCnc.READ_plc_abit(start, end, out data);
    }
}

//------------------------------------------------------------------------------------------------------
// short READ_plc_register(int PlcStart, int PlcEnd, out int[] PlcData)
//------------------------------------------------------------------------------------------------------
public class ReadPlcRegisterFunction : RemoteFunctionModel
{
    public ReadPlcRegisterFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_plc_register";
        Description = "Чтение регистров PLC в указанном диапазоне адресов";
        Signature = "short READ_plc_register(int PlcStart, int PlcEnd, out int[] PlcData)";

        //Добавление тегов
        AddTags("Чтение", "PLC", "Регистры");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "PlcStart", Type = typeof(int), Description = "Начальный адрес регистров PLC", Value = 0
        });

        InputParameters.Add(new Parameter
        {
            Name = "PlcEnd",  Type = typeof(int), Description = "Конечный адрес регистров PLC", Value = 10
        });

       #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "PlcData",  Type = typeof(int[]), Description = "Массив значений регистров PLC"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            int startAddr = (int)InputParameters[0].Value;
            int endAddr = (int)InputParameters[1].Value;

            // Проверка валидности диапазона
            if (startAddr < 0 || endAddr < startAddr || (endAddr - startAddr) > 100)
            {
                throw new ArgumentException("Некорректный диапазон адресов (макс. 100 регистров за запрос)");
            }

            int[] plcData;
            short result = RemoteCnc.READ_plc_register(startAddr, endAddr, out plcData);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = plcData;
                
            }, $"Получены значения регистров PLC в диапазоне [{startAddr} - {endAddr}]");

            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }    
}


//------------------------------------------------------------------------------------------------------
// short READ_plc_register(int Raddress, out string PlcData)
//------------------------------------------------------------------------------------------------------
public class ReadPlcSingleRegisterFunction : RemoteFunctionModel
{
    public ReadPlcSingleRegisterFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_plc_register";
        Description = "Чтение значения регистра PLC по указанному адресу";
        Signature = "short READ_plc_register(int Raddress, out string PlcData)";

        //Добавление тегов
        AddTags("Чтение", "PLC", "Регистры");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "Raddress", Type = typeof(int), Description = "Адрес регистра PLC", Value = 0
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "PlcData",
            Type = typeof(string),
            Description = "Значение регистра"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            int address = (int)InputParameters[0].Value;
            string value;

            short result = RemoteCnc.READ_plc_register(address, out value);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = value;
                
            }, $"Получены значения регистра PLC [{address} : {value}]");

        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short READ_plc_timer(int PlcStart, int PlcEnd, out int[] PlcTimerValue, out int[] PlcTimerSetting, out short[] PlcTimerState)
//------------------------------------------------------------------------------------------------------
public class ReadPlcTimerFunction : RemoteFunctionModel
{
    public ReadPlcTimerFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_plc_timer";
        Description = "Чтение значений и состояний таймеров PLC";
        Signature = "short READ_plc_timer(int PlcStart, int PlcEnd, out int[] PlcTimerValue, out int[] PlcTimerSetting, out short[] PlcTimerState)";

        //Добавление тегов
        AddTags("Чтение", "PLC", "Таймер");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "PlcStart", Type = typeof(int), Description = "Начальный индекс таймера", Value = 0
        });

        InputParameters.Add(new Parameter
        {
            Name = "PlcEnd", Type = typeof(int), Description = "Конечный индекс таймера", Value = 5
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "PlcTimerValue", Type = typeof(int[]), Description = "Текущие значения таймеров (мс)"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "PlcTimerSetting", Type = typeof(int[]), Description = "Установленные значения таймеров (мс)"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "PlcTimerState", Type = typeof(short[]), Description = "Состояния таймеров (0-выкл, 1-вкл, 2-завершен)"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            int start = (int)InputParameters[0].Value;
            int end = (int)InputParameters[1].Value;

            if (start < 0 || end < start)
            {
                throw new ArgumentException("Некорректный диапазон таймеров");
            }

            int[] currentValues, presetValues;
            short[] states;

            short result = RemoteCnc.READ_plc_timer(
                start,
                end,
                out currentValues,
                out presetValues,
                out states
            );

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = currentValues ?? Array.Empty<int>();
                OutputParameters[1].Value = presetValues  ?? Array.Empty<int>();
                OutputParameters[2].Value = states        ?? Array.Empty<short>();
                
            }, $"Получены состояния таймеров");
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short READ_plc_counter(int PlcStart, int PlcEnd, out int[] PlcCounterValue, out int[] PlcCounterSetting, out short[] PlcCounterState)
//------------------------------------------------------------------------------------------------------
public class ReadPlcCounterFunction : RemoteFunctionModel
{
    public ReadPlcCounterFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_plc_counter";
        Description = "Чтение значений и состояний счетчиков PLC";
        Signature = "short READ_plc_counter(int PlcStart, int PlcEnd, out int[] PlcCounterValue, out int[] PlcCounterSetting, out short[] PlcCounterState)";

        //Добавление тегов
        AddTags("Чтение", "PLC", "Счетчик");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "PlcStart", Type = typeof(int), Description = "Начальный индекс счетчика", Value = 0
        });

        InputParameters.Add(new Parameter
        {
            Name = "PlcEnd", Type = typeof(int), Description = "Конечный индекс счетчика", Value = 5
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "PlcCounterValue", Type = typeof(int[]), Description = "Текущие значения счетчиков"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "PlcCounterSetting", Type = typeof(int[]), Description = "Установленные значения счетчиков"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "PlcCounterState", Type = typeof(short[]), Description = "Состояния счетчиков (0-неактивен, 1-активен, 2-достигнут лимит)"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            int start = (int)InputParameters[0].Value;
            int end   = (int)InputParameters[1].Value;

            // Проверка валидности диапазона
            if (start < 0 || end < start || (end - start) > 100)
            {
                throw new ArgumentException("Некорректный диапазон счетчиков (макс. 100 за запрос)");
            }

            int[] currentValues, presetValues;
            short[] states;

            short result = RemoteCnc.READ_plc_counter(
                start,
                end,
                out currentValues,
                out presetValues,
                out states
            );

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = currentValues ?? Array.Empty<int>();
                OutputParameters[1].Value = presetValues ?? Array.Empty<int>();
                OutputParameters[2].Value = states ?? Array.Empty<short>();
                
            }, $"Получены состояния счетчиков");
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}



/// <summary>
/// Абстрактный базовый класс для записи битов PLC (входных, внутренних, статусных)
/// </summary>
public abstract class WritePlcBitsFunction : RemoteFunctionModel
{
    protected WritePlcBitsFunction(SyntecRemoteCNC remoteCnc, string functionName, string description)
        : base(remoteCnc)
    {
        Name = functionName;
        Description = description;
        Signature = $"short {functionName}(int PlcStart, int PlcEnd, byte[] PlcData)";

        //Добавление тегов
        AddTags("Запись", "PLC", "Биты");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "PlcStart",
            Type = typeof(int),
            Description = "Начальный адрес битов",
            Value = 0
        });

        InputParameters.Add(new Parameter
        {
            Name = "PlcEnd",
            Type = typeof(int),
            Description = "Конечный адрес битов",
            Value = 10
        });

        InputParameters.Add(new Parameter
        {
            Name = "BitData",
            Type = typeof(byte[]),
            Description = "Данные для записи (каждый бит соответствует состоянию)",
            Value = new byte[2] 
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "PlcData",
            Type = typeof(int),
            Description = "Количество записанных битов"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            // Получаем параметры
            int startAddr = (int)InputParameters[0].Value;
            int endAddr = (int)InputParameters[1].Value;
            byte[] writeData = InputParameters[2].Value as byte[];

            // Валидация данных
            if (startAddr < 0 || endAddr < startAddr)
            {
                throw new ArgumentException("Некорректный диапазон адресов");
            }

            // Вызов абстрактного метода для выполнения операции записи
            short result = WritePlcBits(startAddr, endAddr, writeData);

            HandleResult(result, () => { }, $"Получены состояния счетчиков");

        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }

    protected abstract short WritePlcBits(int startAddr, int endAddr, byte[] writeData);

}

//------------------------------------------------------------------------------------------------------
// short WRITE_plc_ibit(int PlcStart, int PlcEnd, byte[] PlcData)
//------------------------------------------------------------------------------------------------------
public class WritePlcIbitFunction : WritePlcBitsFunction
{
    public WritePlcIbitFunction(SyntecRemoteCNC remoteCnc)
        : base(remoteCnc, "WRITE_plc_ibit", "Запись значений входных битов PLC")
    {
    }

    protected override short WritePlcBits(int startAddr, int endAddr, byte[] writeData)
    {
        return RemoteCnc.WRITE_plc_ibit(startAddr, endAddr, writeData);
    }
}

//------------------------------------------------------------------------------------------------------
// short WRITE_plc_cbit(int PlcStart, int PlcEnd, byte[] PlcData)
//------------------------------------------------------------------------------------------------------
public class WritePlcCbitFunction : WritePlcBitsFunction
{
    public WritePlcCbitFunction(SyntecRemoteCNC remoteCnc)
        : base(remoteCnc, "WRITE_plc_cbit", "Запись значений внутренних битов PLC")
    {
    }

    protected override short WritePlcBits(int startAddr, int endAddr, byte[] writeData)
    {
        return RemoteCnc.WRITE_plc_cbit(startAddr, endAddr, writeData);
    }
}

//------------------------------------------------------------------------------------------------------
// short WRITE_plc_sbit(int PlcStart, int PlcEnd, byte[] PlcData)
//------------------------------------------------------------------------------------------------------
public class WritePlcSbitFunction : WritePlcBitsFunction
{
    public WritePlcSbitFunction(SyntecRemoteCNC remoteCnc)
        : base(remoteCnc, "WRITE_plc_sbit", "Запись значений статусных битов PLC")
    {
    }

    protected override short WritePlcBits(int startAddr, int endAddr, byte[] writeData)
    {
        return RemoteCnc.WRITE_plc_sbit(startAddr, endAddr, writeData);
    }
}

//------------------------------------------------------------------------------------------------------
// short WRITE_plc_register(int PlcStart, int PlcEnd, int[] PlcData)
//------------------------------------------------------------------------------------------------------
public class WritePlcRegisterFunction : RemoteFunctionModel
{
    public WritePlcRegisterFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "WRITE_plc_register";
        Description = "Запись значений в регистры PLC";
        Signature = "short WRITE_plc_register(int PlcStart, int PlcEnd, int[] PlcData)";

        //Добавление тегов
        AddTags("Запись", "PLC", "Регистры");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "PlcStart", Type = typeof(int), Description = "Начальный адрес регистров", Value = 0
        });

        InputParameters.Add(new Parameter
        {
            Name = "PlcEnd", Type = typeof(int), Description = "Конечный адрес регистров", Value = 10
        });

        InputParameters.Add(new Parameter
        {
            Name = "PlcData", Type = typeof(int[]), Description = "Данные для записи (массив 32-битных значений)", Value = new int[1] // Пустой массив по умолчанию
        });
        #endregion        
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            int startAddr = (int)InputParameters[0].Value;
            int endAddr = (int)InputParameters[1].Value;
            int[] writeData = InputParameters[2].Value as int[];

            // Валидация входных данных
            if (startAddr < 0 || endAddr < startAddr)
            {
                throw new ArgumentException("Некорректный диапазон адресов");
            }

            if (writeData == null || writeData.Length == 0)
            {
                throw new ArgumentException("Нет данных для записи");
            }

            int expectedCount = endAddr - startAddr + 1;
            if (writeData.Length != expectedCount)
            {
                throw new ArgumentException($"Несоответствие количества данных (требуется {expectedCount} значений)");
            }

            // Вызов метода PLC
            short result = RemoteCnc.WRITE_plc_register(startAddr, endAddr, writeData);

            HandleResult(result, () => {}, $"Данные записаны в регистры PLC [{startAddr}-{endAddr}]");
           
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short READ_plc_type(string Addr, out short PlcType)
//------------------------------------------------------------------------------------------------------
public class ReadPlcTypeFunction : RemoteFunctionModel
{
    public ReadPlcTypeFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_plc_type";
        Description = "Определение типа PLC по адресу";
        Signature = "short READ_plc_type(string Addr, out short PlcType)";

        //Добавление тегов
        AddTags("Чтение", "PLC");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "Addr", Type = typeof(string), Description = "Адрес устройства PLC (например, '192.168.1.1:8500')", Value = "localhost"
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "PlcType", Type = typeof(short), Description = "Тип PLC (0-неизвестен, 1-S7-200, 2-S7-300 и т.д.)"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string address = InputParameters[0].Value?.ToString();

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Адрес PLC не указан");
            }

            short plcType;
            short result = RemoteCnc.READ_plc_type(address, out plcType);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = plcType;
                
            }, $"Получен тип PLC по адресу {address} : {plcType}");
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short READ_plc_type2(string Addr, out short PlcType, out int PlcStart, out int PlcEnd)
//------------------------------------------------------------------------------------------------------
public class ReadPlcType2Function : RemoteFunctionModel
{
    public ReadPlcType2Function(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_plc_type2";
        Description = "Определение типа PLC и диапазона адресов";
        Signature = "short READ_plc_type2(string Addr, out short PlcType, out int PlcStart, out int PlcEnd)";

        //Добавление тегов
        AddTags("Чтение", "PLC");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "Addr", Type = typeof(string), Description = "Адрес устройства PLC (IP или hostname)", Value = "localhost"
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "PlcType", Type = typeof(short), Description = "Тип контроллера (1-S7-300, 2-S7-400, ...)"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "PlcStart", Type = typeof(int), Description = "Начальный адрес доступных регистров"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "PlcEnd", Type = typeof(int), Description = "Конечный адрес доступных регистров"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string address = InputParameters[0].Value?.ToString();

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Не указан адрес PLC");
            }

            short plcType;
            int startAddr, endAddr;
            short result = RemoteCnc.READ_plc_type2(address, out plcType, out startAddr, out endAddr);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = plcType;
                OutputParameters[1].Value = startAddr;
                OutputParameters[2].Value = endAddr;
                
            }, $"Получен тип PLC по адресу {address} : {plcType} и его диапазон адресов [{startAddr}-{endAddr}]");
           
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
// short READ_plc_addr(string Addr, int PlcStart, int PlcEnd, out short PlcType, out byte[] PlcDataB, out short[] PlcDataS, out int[] PlcDataI)
//------------------------------------------------------------------------------------------------------
public class ReadPlcAddrFunction : RemoteFunctionModel
{
    public ReadPlcAddrFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_plc_addr";
        Description = "Чтение данных из PLC с автоматическим определением типа данных";
        Signature = "short READ_plc_addr(string Addr, int PlcStart, int PlcEnd, out short PlcType, out byte[] PlcDataB, out short[] PlcDataS, out int[] PlcDataI)";

        //Добавление тегов
        AddTags("Чтение", "PLC");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "Addr", Type = typeof(string), Description = "Адрес устройства PLC", Value = "192.168.1.1:102"
        });

        InputParameters.Add(new Parameter
        {
            Name = "PlcStart", Type = typeof(int), Description = "Начальный адрес для чтения", Value = 0
        });

        InputParameters.Add(new Parameter
        {
            Name = "PlcEnd", Type = typeof(int), Description = "Конечный адрес для чтения", Value = 10
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "PlcType", Type = typeof(short), Description = "Тип PLC (1-S7-300, 2-S7-400, ...)"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "PlcDataB", Type = typeof(byte[]), Description = "Данные в виде массива байт"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "PlcDataS", Type = typeof(short[]), Description = "Данные в виде массива short"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "PlcDataI", Type = typeof(int[]), Description = "Данные в виде массива int"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string address = InputParameters[0].Value?.ToString();
            int startAddr = (int)InputParameters[1].Value;
            int endAddr = (int)InputParameters[2].Value;

            // Валидация параметров
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Не указан адрес PLC");
            }

            if (startAddr < 0 || endAddr < startAddr)
            {
                throw new ArgumentException("Некорректный диапазон адресов");
            }

            short plcType;
            byte[] byteData;
            short[] shortData;
            int[] intData;

            short result = RemoteCnc.READ_plc_addr(
                address,
                startAddr,
                endAddr,
                out plcType,
                out byteData,
                out shortData,
                out intData
            );

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = plcType;
                OutputParameters[1].Value = byteData    ?? Array.Empty<byte>();
                OutputParameters[2].Value = shortData   ?? Array.Empty<short>();
                OutputParameters[3].Value = intData     ?? Array.Empty<int>();
                
            }, $"Получен тип PLC по адресу {address} : {plcType} и его диапазон адресов [{startAddr}-{endAddr}]");

            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short WRITE_plc_addr(string Addr, int PlcStart, int PlcEnd, short PlcType, byte[] PlcDataB, short[] PlcDataS, int[] PlcDataI)
//------------------------------------------------------------------------------------------------------
public class WritePlcAddrFunction : RemoteFunctionModel
{
    public WritePlcAddrFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "WRITE_plc_addr";
        Description = "Запись данных в адресное пространство PLC с указанием типа данных";
        Signature = "short WRITE_plc_addr(string Addr, int PlcStart, int PlcEnd, short PlcType, byte[] PlcDataB, short[] PlcDataS, int[] PlcDataI)";

        //Добавление тегов
        AddTags("Запись", "PLC");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "Addr", Type = typeof(string), Description = "Базовый адрес в PLC", Value = "0"
        });

        InputParameters.Add(new Parameter
        {
            Name = "PlcStart", Type = typeof(int), Description = "Начальный индекс данных", Value = 0
        });

        InputParameters.Add(new Parameter
        {
            Name = "PlcEnd", Type = typeof(int), Description = "Конечный индекс данных", Value = 10
        });

        InputParameters.Add(new Parameter
        {
            Name = "PlcType", Type = typeof(short), Description = "Тип данных (0 - byte, 1 - short, 2 - int)", Value = (short)0
        });

        InputParameters.Add(new Parameter
        {
            Name = "PlcDataB", Type = typeof(byte[]), Description = "Данные для записи (byte)", Value = new byte[0]
        });

        InputParameters.Add(new Parameter
        {
            Name = "PlcDataS", Type = typeof(short[]), Description = "Данные для записи (short)", Value = new short[0]
        });

        InputParameters.Add(new Parameter
        {
            Name = "PlcDataI", Type = typeof(int[]), Description = "Данные для записи (int)", Value = new int[0]
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string address = InputParameters[0].Value?.ToString();
            int start = (int)InputParameters[1].Value;
            int end = (int)InputParameters[2].Value;
            short type = (short)InputParameters[3].Value;
            byte[] dataB = InputParameters[4].Value as byte[];
            short[] dataS = InputParameters[5].Value as short[];
            int[] dataI = InputParameters[6].Value as int[];

           
            // Вызов метода PLC
            short result = RemoteCnc.WRITE_plc_addr(address, start, end, type, dataB, dataS, dataI);

            HandleResult(result, () => { }, $"Данные записаны в адресное пространство PLC [{start}-{end}]");
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short READ_plc_ver(out string Version)
//------------------------------------------------------------------------------------------------------
public class ReadPlcVersionFunction : RemoteFunctionModel
{
    public ReadPlcVersionFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_plc_ver";
        Description = "Чтение версии программного обеспечения PLC";
        Signature = "short READ_plc_ver(out string Version)";

        //Добавление тегов
        AddTags("Чтение", "PLC");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "Version", Type = typeof(string), Description = "Версия программного обеспечения PLC"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string version;
            short result = RemoteCnc.READ_plc_ver(out version);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = version ?? "Неизвестно";
                
            }, $"Получена текущая версия PLC");
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
// short DOWNLOAD_plc_ladder(string Destination)
//------------------------------------------------------------------------------------------------------
public class DownloadPlcLadderFunction : RemoteFunctionModel
{
    public DownloadPlcLadderFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "DOWNLOAD_plc_ladder";
        Description = "Скачивание программы PLC (лестничной логики) на указанный путь";
        Signature = "short DOWNLOAD_plc_ladder(string Destination)";

        //Добавление тегов
        AddTags("Скачать", "PLC");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "DestinationPath", Type = typeof(string),
            Description = "Локальный путь для сохранения файла программы PLC",
            Value = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string destinationPath = InputParameters[0].Value?.ToString();

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new ArgumentException("Не указан путь для сохранения");
            }

            // Добавляем имя файла, если указана только папка
            if (Directory.Exists(destinationPath) && !Path.HasExtension(destinationPath))
            {
                destinationPath = Path.Combine(destinationPath, $"PLC_Ladder_{DateTime.Now:yyyyMMdd_HHmmss}.ldr");
            }

            short result = RemoteCnc.DOWNLOAD_plc_ladder(destinationPath);

            HandleResult(result, () => { }, $"Программа PLC сохранена по пути : {destinationPath}");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short READ_debug_variable(int DebugStart, int DebugEnd, out int[] DebugVariable)
//------------------------------------------------------------------------------------------------------
public class ReadDebugVariableFunction : RemoteFunctionModel
{
    public ReadDebugVariableFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_debug_variable";
        Description = "Чтение отладочных переменных в указанном диапазоне";
        Signature = "short READ_debug_variable(int DebugStart, int DebugEnd, out int[] DebugVariable)";

        //Добавление тегов
        AddTags("Чтение", "Система", "Переменные" );

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "DebugStart", Type = typeof(int), Description = "Начальный индекс отладочной переменной", Value = 0
        });

        InputParameters.Add(new Parameter
        {
            Name = "DebugEnd", Type = typeof(int), Description = "Конечный индекс отладочной переменной", Value = 10
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "DebugVariables", Type = typeof(int[]), Description = "Массив значений отладочных переменных"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            int start = (int)InputParameters[0].Value;
            int end = (int)InputParameters[1].Value;

            // Проверка валидности диапазона
            if (start < 0 || end < start)
            {
               throw new ArgumentException("Некорректный диапазон переменных");
            }

            int[] debugVariables;
            short result = RemoteCnc.READ_debug_variable(start, end, out debugVariables);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = debugVariables ?? Array.Empty<int>();
                
            }, $"Получены отладочные переменные в диапазоне [{start}:{end}] ");
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short READ_system_variable(int SystemID, int SystemStart, int SystemEnd, out double[] SystemVariable)
//------------------------------------------------------------------------------------------------------
public class ReadSystemVariableFunction : RemoteFunctionModel
{
    public ReadSystemVariableFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_system_variable";
        Description = "Чтение системных переменных по идентификатору системы в указанном диапазоне";
        Signature = "short READ_system_variable(int SystemID, int SystemStart, int SystemEnd, out double[] SystemVariable)";

        //Добавление тегов
        AddTags("Чтение", "Система", "Переменные");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "SystemID", Type = typeof(int), Description = "Идентификатор системы", Value = 1
        });

        InputParameters.Add(new Parameter
        {
            Name = "SystemStart", Type = typeof(int), Description = "Начальный индекс системной переменной", Value = 0
        });

        InputParameters.Add(new Parameter
        {
            Name = "SystemEnd", Type = typeof(int), Description = "Конечный индекс системной переменной", Value = 10
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "SystemVariables", Type = typeof(double[]), Description = "Массив значений системных переменных"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            int systemId = (int)InputParameters[0].Value;
            int start = (int)InputParameters[1].Value;
            int end = (int)InputParameters[2].Value;

            // Проверка валидности диапазона
            if (start < 0 || end < start)
            {
                throw new ArgumentException("Некорректный диапазон переменных");
            }

            double[] systemVariables;
            short result = RemoteCnc.READ_system_variable(systemId, start, end, out systemVariables);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = systemVariables ?? Array.Empty<double>();
                
            }, $"Получены системные переменные в диапазоне [{start}:{end}] ");
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short READ_offset_title(out string[] OffsetTitle)
//------------------------------------------------------------------------------------------------------
public class ReadOffsetTitleFunction : RemoteFunctionModel
{
    public ReadOffsetTitleFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_offset_title";
        Description = "Чтение заголовков/наименований смещений инструмента";
        Signature = "short READ_offset_title(out string[] OffsetTitle)";

        //Добавление тегов
        AddTags("Чтение", "Инструмент");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "OffsetTitle", Type = typeof(string[]), Description = "Массив наименований смещений инструмента"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string[] offsetTitles;
            short result = RemoteCnc.READ_offset_title(out offsetTitles);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = offsetTitles ?? Array.Empty<string>();
                
            }, $"Получены заголовки/наименования смещений инструмента"); 
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }    
}


//------------------------------------------------------------------------------------------------------
// short READ_offset_count(out short Count)
//------------------------------------------------------------------------------------------------------
public class ReadOffsetCountFunction : RemoteFunctionModel
{
    public ReadOffsetCountFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_offset_count";
        Description = "Получение количества доступных смещений инструмента";
        Signature = "short READ_offset_count(out short Count)";

        //Добавление тегов
        AddTags("Чтение", "Инструмент");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "Count", Type = typeof(short), Description = "Количество доступных смещений инструмента"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            short count;
            short result = RemoteCnc.READ_offset_count(out count);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = count;
                
            }, $"Получено число доступных смещений инструмента [{count}]");
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
// short READ_offset_all(out double[][] OffsetData)
//------------------------------------------------------------------------------------------------------
public class ReadOffsetAllFunction : RemoteFunctionModel
{
    public ReadOffsetAllFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_offset_all";
        Description = "Чтение всех данных смещений инструмента";
        Signature = "short READ_offset_all(out double[][] OffsetData)";

        //Добавление тегов
        AddTags("Чтение", "Инструмент");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "OffsetData", Type = typeof(double[][]), Description = "Массив массивов данных смещений инструмента"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            double[][] offsetData;
            short result = RemoteCnc.READ_offset_all(out offsetData);


            HandleResult(result, () =>
            {
                OutputParameters[0].Value = offsetData ?? Array.Empty<double[]>();
                
            }, $"Получены доступные смещений инструмента");
           
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
// short READ_offset_scope(short StartNumber, short EndNumber, out double[][] OffsetData)
//------------------------------------------------------------------------------------------------------
public class ReadOffsetScopeFunction : RemoteFunctionModel
{
    public ReadOffsetScopeFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_offset_scope";
        Description = "Чтение данных смещений инструмента в указанном диапазоне";
        Signature = "short READ_offset_scope(short StartNumber, short EndNumber, out double[][] OffsetData)";

        //Добавление тегов
        AddTags("Чтение", "Инструмент");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "StartNumber", Type = typeof(short), Description = "Начальный номер смещения", Value = (short)1
        });

        InputParameters.Add(new Parameter
        {
            Name = "EndNumber", Type = typeof(short), Description = "Конечный номер смещения", Value = (short)10
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "OffsetData",
            Type = typeof(double[][]),
            Description = "Массив массивов данных смещений инструмента"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            short start = (short)InputParameters[0].Value;
            short end = (short)InputParameters[1].Value;

            // Валидация диапазона
            if (start < 0 || end < start)
            {
                throw new ArgumentException("Некорректный диапазон смещений");
            }

            double[][] offsetData;
            short result = RemoteCnc.READ_offset_scope(start, end, out offsetData);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = offsetData ?? Array.Empty<double[]>();
                
            }, $"Получены данные о смещении инструмента в диапазоне [{start}:{end}]");

        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short READ_offset_single(short ofNumber, out double[] OffsetData)
//------------------------------------------------------------------------------------------------------
public class ReadOffsetSingleFunction : RemoteFunctionModel
{
    public ReadOffsetSingleFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_offset_single";
        Description = "Чтение данных конкретного смещения инструмента по номеру";
        Signature = "short READ_offset_single(short ofNumber, out double[] OffsetData)";

        //Добавление тегов
        AddTags("Чтение", "Инструмент");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "ofNumber", Type = typeof(short), Description = "Номер смещения инструмента", Value = (short)1  
        });
        #endregion

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "OffsetData", Type = typeof(double[]), Description = "Массив значений смещения по осям"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            short offsetNumber = (short)InputParameters[0].Value;
            double[] offsetData;
            short result = RemoteCnc.READ_offset_single(offsetNumber, out offsetData);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = offsetData ?? Array.Empty<double>();
                
            }, $"Получены данные о смещении инструмента по номеру [{offsetNumber}]");
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short WRITE_offset_all(double[][] OffsetData)
//------------------------------------------------------------------------------------------------------
public class WriteOffsetAllFunction : RemoteFunctionModel
{
    public WriteOffsetAllFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "WRITE_offset_all";
        Description = "Запись данных смещений для всех осей";
        Signature = "short WRITE_offset_all(double[][] OffsetData)";

        //Добавление тегов
        AddTags("Запись", "Инструмент");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "OffsetData", Type = typeof(double[][]), Description = "Двумерный массив данных смещений [ось][значение]", Value = new double[0][] // Пустой массив по умолчанию
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            double[][] offsetData = InputParameters[0].Value as double[][];

            // Проверка входных данных
            if (offsetData == null || offsetData.Length == 0)
            {
                throw new ArgumentException("Нет данных смещений для записи");
            }

            // Вызов метода ЧПУ
            short result = RemoteCnc.WRITE_offset_all(offsetData);

            HandleResult(result, () => { }, $"Данные о смещении для всех осей записаны");
           
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short WRITE_offset_single(short ofNumber, double[] OffsetData)
//------------------------------------------------------------------------------------------------------
public class WriteOffsetSingleFunction : RemoteFunctionModel
{
    public WriteOffsetSingleFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "WRITE_offset_single";
        Description = "Запись данных смещения для одной оси";
        Signature = "short WRITE_offset_single(short ofNumber, double[] OffsetData)";

        //Добавление тегов
        AddTags("Запись", "Инструмент");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "OffsetNumber", Type = typeof(short), Description = "Номер смещения", Value = (short)1 
        });

        InputParameters.Add(new Parameter
        {
            Name = "OffsetData", Type = typeof(double[]), Description = "Массив значений смещения для оси",
            Value = new double[3] { 0.0, 0.0, 0.0 } // Значения по умолчанию для X,Y,Z
        });
        #endregion

    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            // Получаем входные параметры
            short offsetNumber = (short)InputParameters[0].Value;
            double[] offsetData = InputParameters[1].Value as double[];

            // Проверка данных
            if (offsetData == null || offsetData.Length == 0)
            {
                throw new ArgumentException("Нет данных смещения для записи");
            }

            // Вызов метода ЧПУ
            short result = RemoteCnc.WRITE_offset_single(offsetNumber, offsetData);

            HandleResult(result, () => { }, $"Данные о смещении для оси [{offsetNumber}] записаны");

        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short READ_nc_OPLog(out string[] OPLog, ref int count)
//------------------------------------------------------------------------------------------------------
public class ReadNcOpLogFunction : RemoteFunctionModel
{
    public ReadNcOpLogFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_nc_OPLog";
        Description = "Чтение журнала операций ЧПУ";
        Signature = "short READ_nc_OPLog(out string[] OPLog, ref int count)";

        //Добавление тегов
        AddTags("Чтение", "NC");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "OPLog", Type = typeof(string[]), Description = "Массив записей журнала операций"
        });

        OutputParameters.Add(new Parameter
        {
            Name = "count", Type = typeof(int), Description = "Количество записей в журнале"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string[] opLog;
            int count = 0;

            // Вызов метода ЧПУ
            short result = RemoteCnc.READ_nc_OPLog(out opLog, ref count);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = opLog ?? Array.Empty<string>();
                OutputParameters[1].Value = count;
                
            }, $"Получен журнал операций ЧПУ");
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }

}


//------------------------------------------------------------------------------------------------------
// short WRITE_nc_main(string NcName)
//------------------------------------------------------------------------------------------------------
public class WriteNcMainFunction : RemoteFunctionModel
{
    public WriteNcMainFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "WRITE_nc_main";
        Description = "Запись имени главной NC программы";
        Signature = "short WRITE_nc_main(string NcName)";

        //Добавление тегов
        AddTags("Запись", "NC");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "NcName", Type = typeof(string), Description = "Имя NC программы для установки в качестве главной",
            Value = string.Empty         });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string programName = InputParameters[0].Value?.ToString();

            // Проверка входных данных
            if (string.IsNullOrWhiteSpace(programName))
            {
                throw new ArgumentException("Не указано имя программы");
            }

            // Вызов метода ЧПУ
            short result = RemoteCnc.WRITE_nc_main(programName);

            HandleResult(result, () => { }, $"Имя главной NC программы изменено");
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}

//------------------------------------------------------------------------------------------------------
// short READ_nc_pointer(out int BlkPointer)
//------------------------------------------------------------------------------------------------------
public class ReadNcPointerFunction : RemoteFunctionModel
{
    public ReadNcPointerFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_nc_pointer";
        Description = "Чтение указателя текущего блока NC программы";
        Signature = "short READ_nc_pointer(out int BlkPointer)";

        //Добавление тегов
        AddTags("Чтение", "NC");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "BlkPointer", Type = typeof(int), Description = "Текущая позиция указателя блока в NC программе"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            int blockPointer;
            short result = RemoteCnc.READ_nc_pointer(out blockPointer);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = blockPointer;
                
            }, $"Получен указатель текущего блока NC программы");
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short READ_nc_current_block(out string Block)
//------------------------------------------------------------------------------------------------------
public class ReadNcCurrentBlockFunction : RemoteFunctionModel
{
    public ReadNcCurrentBlockFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_nc_current_block";
        Description = "Чтение текущего блока NC программы";
        Signature = "short READ_nc_current_block(out string Block)";

        //Добавление тегов
        AddTags("Чтение", "NC");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "Block", Type = typeof(string), Description = "Текущий выполняемый блок NC кода"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string currentBlock;
            short result = RemoteCnc.READ_nc_current_block(out currentBlock);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = currentBlock ?? string.Empty;
                
            }, $"Получен текущий блока NC программы");

        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }

}

//------------------------------------------------------------------------------------------------------
// short READ_nc_freespace(out long FreeSpace)
//------------------------------------------------------------------------------------------------------
public class ReadNcFreeSpaceFunction : RemoteFunctionModel
{
    public ReadNcFreeSpaceFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_nc_freespace";
        Description = "Чтение информации о свободном месте в памяти ЧПУ";
        Signature = "short READ_nc_freespace(out long FreeSpace)";

        //Добавление тегов
        AddTags("Чтение", "NC");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "FreeSpace", Type = typeof(long), Description = "Количество свободных байт в памяти ЧПУ"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            long freeSpace;
            short result = RemoteCnc.READ_nc_freespace(out freeSpace);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = freeSpace;
                
            }, $"Получена информация о свободном месте в памяти ЧПУ [{freeSpace}]");

            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short READ_nc_mem_list(out string[][] NcList)
//------------------------------------------------------------------------------------------------------
public class ReadNcMemListFunction : RemoteFunctionModel
{
    public ReadNcMemListFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_nc_mem_list";
        Description = "Чтение списка NC программ из памяти ЧПУ";
        Signature = "short READ_nc_mem_list(out string[][] NcList)";

        //Добавление тегов
        AddTags("Чтение", "NC");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "NcList", Type = typeof(string[][]), Description = "Массив массивов строк с информацией о программах"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            string[][] ncList;
            short result = RemoteCnc.READ_nc_mem_list(out ncList);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = ncList ?? Array.Empty<string[]>();
                
            }, $"Получен список NC программ из памяти ЧПУ");

        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
}


//------------------------------------------------------------------------------------------------------
// short READ_remoteTime(out DateTime remoteTime)
//------------------------------------------------------------------------------------------------------
public class ReadRemoteTimeFunction : RemoteFunctionModel
{
    public ReadRemoteTimeFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "READ_remoteTime";
        Description = "Чтение текущего времени с удаленного ЧПУ";
        Signature = "short READ_remoteTime(out DateTime remoteTime)";

        //Добавление тегов
        AddTags("Чтение", "Время");

        #region Инициализация выходных параметров
        OutputParameters.Add(new Parameter
        {
            Name = "remoteTime", Type = typeof(DateTime), Description = "Текущее время на ЧПУ"
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            DateTime remoteTime;
            short result = RemoteCnc.READ_remoteTime(out remoteTime);

            HandleResult(result, () =>
            {
                OutputParameters[0].Value = remoteTime;
                
            }, $"Получено время с удаленного ЧПУ [{remoteTime.ToLongTimeString()}]");

        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }
       
}


//------------------------------------------------------------------------------------------------------
// short WRITE_remoteDate(int Year, int Month, int Day)
//------------------------------------------------------------------------------------------------------
public class WriteRemoteDateFunction : RemoteFunctionModel
{
    public WriteRemoteDateFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "WRITE_remoteDate";
        Description = "Установка даты на удаленном ЧПУ";
        Signature = "short WRITE_remoteDate(int Year, int Month, int Day)";

        //Добавление тегов
        AddTags("Запись", "Время");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "Year", Type = typeof(int), Description = "Год (например, 2023)", Value = DateTime.Now.Year
        });

        InputParameters.Add(new Parameter
        {
            Name = "Month", Type = typeof(int), Description = "Месяц (1-12)", Value = DateTime.Now.Month
        });

        InputParameters.Add(new Parameter
        {
            Name = "Day", Type = typeof(int), Description = "День (1-31)", Value = DateTime.Now.Day
        });
        #endregion
    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            // Получаем входные параметры
            int year = (int)InputParameters[0].Value;
            int month = (int)InputParameters[1].Value;
            int day = (int)InputParameters[2].Value;

            // Проверка валидности даты
            if (!IsValidDate(year, month, day))
            {
                throw new ArgumentException("Некорректная дата");
            }

            // Вызов метода ЧПУ
            short result = RemoteCnc.WRITE_remoteDate(year, month, day);

            HandleResult(result, () => { }, $"Дата установлена на ЧПУ [{day}.{month}.{year}]");


        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }

    private bool IsValidDate(int year, int month, int day)
    {
        if (year < 1975 || year > 2200)
            return false;

        if (month < 1 || month > 12)
            return false;

        if (day < 1 || day > DateTime.DaysInMonth(year, month))
            return false;

        return true;
    }
}


//------------------------------------------------------------------------------------------------------
// short WRITE_remoteTime(int Hour, int Minute, int Second)
//------------------------------------------------------------------------------------------------------
public class WriteRemoteTimeFunction : RemoteFunctionModel
{
    public WriteRemoteTimeFunction(SyntecRemoteCNC remoteCnc) : base(remoteCnc)
    {
        Name = "WRITE_remoteTime";
        Description = "Установка времени на удаленном ЧПУ";
        Signature = "short WRITE_remoteTime(int Hour, int Minute, int Second)";

        //Добавление тегов
        AddTags("Запись", "Время");

        #region Инициализация входных параметров
        InputParameters.Add(new Parameter
        {
            Name = "Hour", Type = typeof(int), Description = "Часы (0-23)", Value = DateTime.Now.Hour
        });

        InputParameters.Add(new Parameter
        {
            Name = "Minute", Type = typeof(int), Description = "Минуты (0-59)", Value = DateTime.Now.Minute
        });

        InputParameters.Add(new Parameter
        {
            Name = "Second", Type = typeof(int), Description = "Секунды (0-59)", Value = DateTime.Now.Second
        });
        #endregion

    }

    public override void Execute()
    {
        if (!CanExecute())
            return;

        try
        {
            // Получаем входные параметры
            int hour = (int)InputParameters[0].Value;
            int minute = (int)InputParameters[1].Value;
            int second = (int)InputParameters[2].Value;

            // Проверка валидности времени
            if (!IsValidTime(hour, minute, second))
            {
                throw new ArgumentException("Некорректное время");
            }

            // Вызов метода ЧПУ
            short result = RemoteCnc.WRITE_remoteTime(hour, minute, second);

            HandleResult(result, () => { }, $"Время установлено на ЧПУ [{hour}.{minute}.{second}]");
            
        }
        catch (Exception ex)
        {
            Result = ExceptionResult(ex);
            OnNotification($"Критическая ошибка : {ex.Message}", NotificationType.Error);
        }
    }

    private bool IsValidTime(int hour, int minute, int second)
    {
        return hour >= 0 && hour < 24 &&
               minute >= 0 && minute < 60 &&
               second >= 0 && second < 60;
    }   
}