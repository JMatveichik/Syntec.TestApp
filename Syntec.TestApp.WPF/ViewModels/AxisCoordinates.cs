using System;
using System.Threading.Tasks;
using ReactiveUI;
using Syntec.Remote;

namespace Syntec.TestApp.WPF.ViewModels
{
    /// <summary>
    /// Модель текущих координат осей ЧПУ.
    /// Поддерживает реактивное обновление при изменении данных.
    /// </summary>
    public class AxisCoordinates : ReactiveCncModel
    {
        private string[] _axisNames     = Array.Empty<string>();
        private string[] _units         = Array.Empty<string>();
        private float[] _machineCoords  = Array.Empty<float>();
        private float[] _absoluteCoords = Array.Empty<float>();
        private float[] _relativeCoords = Array.Empty<float>();
        private float[] _distanceToGo   = Array.Empty<float>();
        private short _decimalPlaces = 3;
        private bool _isPolling = false;

        /// <summary>
        /// Массив имён осей в текущей системе координат.
        /// </summary>
        public string[] AxisNames
        {
            get => _axisNames;
            private set => this.RaiseAndSetIfChanged(ref _axisNames, value);
        }

        /// <summary>
        /// Количество знаков после запятой для отображения координат.
        /// </summary>
        public short DecimalPlaces
        {
            get => _decimalPlaces;
            private set => this.RaiseAndSetIfChanged(ref _decimalPlaces, value);
        }

        /// <summary>
        /// Единицы измерения для каждой оси (mm, inch, etc).
        /// </summary>
        public string[] Units
        {
            get => _units;
            private set => this.RaiseAndSetIfChanged(ref _units, value);
        }

        /// <summary>
        /// Машинные координаты (абсолютные относительно нуля станка).
        /// </summary>
        public float[] MachineCoords
        {
            get => _machineCoords;
            private set => this.RaiseAndSetIfChanged(ref _machineCoords, value);
        }

        /// <summary>
        /// Абсолютные координаты в текущей системе координат.
        /// </summary>
        public float[] AbsoluteCoords
        {
            get => _absoluteCoords;
            private set => this.RaiseAndSetIfChanged(ref _absoluteCoords, value);
        }

        /// <summary>
        /// Относительные координаты (относительно текущей позиции).
        /// </summary>
        public float[] RelativeCoords
        {
            get => _relativeCoords;
            private set => this.RaiseAndSetIfChanged(ref _relativeCoords, value);
        }

        /// <summary>
        /// Оставшееся расстояние до целевой позиции.
        /// </summary>
        public float[] DistanceToGo
        {
            get => _distanceToGo;
            private set => this.RaiseAndSetIfChanged(ref _distanceToGo, value);
        }
        
        public bool IsPolling
        {
            get => _isPolling;
            private set => this.RaiseAndSetIfChanged(ref _isPolling, value);
        }

        public async Task StartPollingAsync(TimeSpan interval)
        {
            if (IsPolling) 
                return;

            IsPolling = true;

            try
            {
                while (IsPolling && RemoteCnc?.isConnected() == true)
                {
                    UpdateInternalState();
                    await Task.Delay(interval);
                }
            }
            finally
            {
                IsPolling = false;
            }
        }

        public void StopPolling()
        {
            IsPolling = false;
        }

        public AxisCoordinates(SyntecRemoteCNC remoteCnc) : base(remoteCnc) { }

        public override void UpdateInternalState()
        {
            try
            {
                if (RemoteCnc?.isConnected() != true)
                {
                    ResetToDefault();
                    return;
                }

                short result = RemoteCnc.READ_position(
                    out string[] axisNames,
                    out short decimals,
                    out string[] units,
                    out float[] machine,
                    out float[] absolute,
                    out float[] relative,
                    out float[] distance);

                if (result == (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
                {
                    AxisNames = axisNames ?? Array.Empty<string>();
                    DecimalPlaces = decimals;
                    Units = units ?? Array.Empty<string>();
                    MachineCoords = machine ?? Array.Empty<float>();
                    AbsoluteCoords = absolute ?? Array.Empty<float>();
                    RelativeCoords = relative ?? Array.Empty<float>();
                    DistanceToGo = distance ?? Array.Empty<float>();
                }
            }
            catch
            {
                ResetToDefault();
            }
        }

        private void ResetToDefault()
        {
            AxisNames = Array.Empty<string>();
            DecimalPlaces = 0;
            Units = Array.Empty<string>();
            MachineCoords = Array.Empty<float>();
            AbsoluteCoords = Array.Empty<float>();
            RelativeCoords = Array.Empty<float>();
            DistanceToGo = Array.Empty<float>();
        }
    }
}