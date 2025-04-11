using System;
using ReactiveUI;
using Syntec.Remote;

namespace Syntec.TestApp.WPF.ViewModels
{
    /// <summary>
    /// Модель, содержащая информацию о подключённом ЧПУ Syntec.
    /// Автоматически обновляет данные при изменении подключения.
    /// </summary>
    public class CncInfo : ReactiveCncModel
    {
        private short _axes;
        private string _cncType = "UNKNOWN";
        private short _maxAxes;
        private string _series = "UNKNOWN";
        private string _ncVersion = "UNKNOWN";
        private string[] _axisNames = Array.Empty<string>();

        /// <summary>
        /// Количество активных осей ЧПУ.
        /// </summary>
        public short Axes
        {
            get => _axes;
            private set => this.RaiseAndSetIfChanged(ref _axes, value);
        }

        /// <summary>
        /// Тип ЧПУ (например, "Syntec EZ4").
        /// </summary>
        public string CncType
        {
            get => _cncType;
            private set => this.RaiseAndSetIfChanged(ref _cncType, value);
        }

        /// <summary>
        /// Максимальное количество поддерживаемых осей.
        /// </summary>
        public short MaxAxes
        {
            get => _maxAxes;
            private set => this.RaiseAndSetIfChanged(ref _maxAxes, value);
        }

        /// <summary>
        /// Серия ЧПУ (например, "EZ Series").
        /// </summary>
        public string Series
        {
            get => _series;
            private set => this.RaiseAndSetIfChanged(ref _series, value);
        }

        /// <summary>
        /// Версия ПО ЧПУ.
        /// </summary>
        public string NcVersion
        {
            get => _ncVersion;
            private set => this.RaiseAndSetIfChanged(ref _ncVersion, value);
        }

        /// <summary>
        /// Массив имён осей (например, ["X", "Y", "Z"]).
        /// </summary>
        public string[] AxisNames
        {
            get => _axisNames;
            private set => this.RaiseAndSetIfChanged(ref _axisNames, value);
        }

        /// <summary>
        /// Инициализирует модель информации о ЧПУ.
        /// </summary>
        /// <param name="remoteCnc">Подключение к ЧПУ Syntec.</param>
        public CncInfo(SyntecRemoteCNC remoteCnc) : base(remoteCnc) 
        {
            OnNotification($"Инициализирован сбор информации о ЧПУ");
        }

        /// <summary>
        /// Обновляет внутреннее состояние модели, запрашивая актуальные данные с ЧПУ.
        /// Вызывается автоматически при изменении RemoteCnc.
        /// </summary>
        public override void UpdateInternalState()
        {
            try
            {
                if (RemoteCnc?.isConnected() != true)
                {
                    OnNotification("Нет подключения к ЧПУ", NotificationType.Warning);
                    ResetToDefault();
                    return;
                }

                OnNotification("Запрос информации о ЧПУ...", NotificationType.Debug);

                short result = RemoteCnc.READ_information(
                    out short axes,
                    out string cncType,
                    out short maxAxes,
                    out string series,
                    out string ncVersion,
                    out string[] axisNames);

                if (result == (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
                {
                    Axes = axes;
                    CncType = cncType;
                    MaxAxes = maxAxes;
                    Series = series;
                    NcVersion = ncVersion;
                    AxisNames = axisNames ?? Array.Empty<string>();

                    OnNotification($"Получена информация о ЧПУ: {CncType} v{NcVersion}");
                }
                else
                {
                    OnNotification($"Ошибка чтения информации. Код: {result}", NotificationType.Error);
                    throw new Exception($"Ошибка чтения информации. Код: {result}");
                }
            }
            catch (Exception ex)
            {
                OnNotification($"Ошибка при обновлении информации: {ex.Message}", NotificationType.Error);  
                ResetToDefault();
                throw ex;
            }
        }

        /// <summary>
        /// Сбрасывает все свойства модели в значения по умолчанию.
        /// </summary>
        private void ResetToDefault()
        {
            Axes = 0;
            CncType = "UNKNOWN";
            MaxAxes = 0;
            Series = "UNKNOWN";
            NcVersion = "UNKNOWN";
            AxisNames = Array.Empty<string>();
        }
    }
}