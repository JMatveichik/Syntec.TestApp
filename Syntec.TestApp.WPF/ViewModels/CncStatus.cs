using System;
using ReactiveUI;
using Syntec.Remote;

namespace Syntec.TestApp.WPF.ViewModels
{
    /// <summary>
    /// Модель текущего статуса ЧПУ Syntec.
    /// Автоматически обновляется при изменении состояния подключения.
    /// </summary>
    public class CncStatus : ReactiveCncModel
    {
        private string _mainProgram = "N/A";
        private string _currentProgram = "N/A";
        private int _currentSequence;
        private string _mode = "N/A";
        private string _status = "N/A";
        private string _alarm = "N/A";
        private string _emergencyStop = "N/A";

        /// <summary>
        /// Имя главной выполняемой программы.
        /// </summary>
        public string MainProgram
        {
            get => _mainProgram;
            private set => this.RaiseAndSetIfChanged(ref _mainProgram, value);
        }

        /// <summary>
        /// Имя текущей выполняемой подпрограммы.
        /// </summary>
        public string CurrentProgram
        {
            get => _currentProgram;
            private set => this.RaiseAndSetIfChanged(ref _currentProgram, value);
        }

        /// <summary>
        /// Номер текущего выполняемого кадра (G-кода).
        /// </summary>
        public int CurrentSequence
        {
            get => _currentSequence;
            private set => this.RaiseAndSetIfChanged(ref _currentSequence, value);
        }

        /// <summary>
        /// Текущий режим работы ЧПУ (ручной, автоматический и т.д.).
        /// </summary>
        public string Mode
        {
            get => _mode;
            private set => this.RaiseAndSetIfChanged(ref _mode, value);
        }

        /// <summary>
        /// Общее состояние системы (работает, на паузе, в ошибке).
        /// </summary>
        public string Status
        {
            get => _status;
            private set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        /// <summary>
        /// Текущие аварийные сообщения, если есть.
        /// </summary>
        public string Alarm
        {
            get => _alarm;
            private set => this.RaiseAndSetIfChanged(ref _alarm, value);
        }

        /// <summary>
        /// Состояние аварийной остановки.
        /// </summary>
        public string EmergencyStop
        {
            get => _emergencyStop;
            private set => this.RaiseAndSetIfChanged(ref _emergencyStop, value);
        }

        public CncStatus(SyntecRemoteCNC remoteCnc) : base(remoteCnc) { }

        /// <summary>
        /// Обновляет статусную информацию с ЧПУ.
        /// </summary>
        public override void UpdateInternalState()
        {
            try
            {
                if (RemoteCnc?.isConnected() != true)
                {
                    ResetToDefault();
                    return;
                }

                short result = RemoteCnc.READ_status(
                    out string mainProg,
                    out string curProg,
                    out int curSeq,
                    out string mode,
                    out string status,
                    out string alarm,
                    out string emg);

                if (result == (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
                {
                    MainProgram = mainProg ?? "N/A";
                    CurrentProgram = curProg ?? "N/A";
                    CurrentSequence = curSeq;
                    Mode = mode ?? "N/A";
                    Status = status ?? "N/A";
                    Alarm = alarm ?? "N/A";
                    EmergencyStop = emg ?? "N/A";
                }
            }
            catch
            {
                ResetToDefault();
            }
        }

        private void ResetToDefault()
        {
            MainProgram = "N/A";
            CurrentProgram = "N/A";
            CurrentSequence = 0;
            Mode = "N/A";
            Status = "N/A";
            Alarm = "N/A";
            EmergencyStop = "N/A";
        }
    }
}