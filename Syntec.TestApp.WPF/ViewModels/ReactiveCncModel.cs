using ReactiveUI;
using Syntec.Remote;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Syntec.TestApp.WPF.ViewModels
{
    /// <summary>
    /// Абстрактный базовый класс для всех моделей данных ЧПУ Syntec.
    /// Автоматически обновляет состояние при изменении подключения к ЧПУ.
    /// </summary>
    public abstract class ReactiveCncModel : ReactiveBaseModel
    {
        private SyntecRemoteCNC _remoteCnc;

        /// <summary>
        /// Текущее подключение к ЧПУ. При изменении автоматически вызывает обновление состояния модели.
        /// </summary>
        public SyntecRemoteCNC RemoteCnc
        {
            get => _remoteCnc;
            set
            {
                if (_remoteCnc != value)
                {
                    this.RaiseAndSetIfChanged(ref _remoteCnc, value);
                    UpdateInternalState(); // Явное обновление при ручном изменении
                }
            }
        }

        /// <summary>
        /// Инициализирует модель с указанным подключением к ЧПУ.
        /// </summary>
        /// <param name="remoteCnc">Экземпляр подключения к ЧПУ Syntec.</param>
        protected ReactiveCncModel(SyntecRemoteCNC remoteCnc) : base()
        {
            RemoteCnc = remoteCnc;

            // Автоматическое обновление при изменении состояния подключения
            this.WhenAnyValue(x => x.RemoteCnc)
                .Skip(1) // Пропускаем инициализацию
                .Subscribe(_ => UpdateInternalState())
                .DisposeWith(Disposables);
        }

        /// <summary>
        /// Абстрактный метод для обновления внутреннего состояния модели.
        /// Должен быть реализован в производных классах.
        /// </summary>
        public abstract void UpdateInternalState();
        
    }
}