using System;
using System.Linq;
using ReactiveUI;
using System.Reactive.Disposables;
using Syntec.TestApp.WPF.ViewModels;
using System.Reactive.Linq;

namespace Syntec.TestApp.WPF.Views
{
    /// <summary>
    /// Interaction logic for LogView.xaml
    /// </summary>
    public partial class LogView : ReactiveUserControl<LogViewModel>
    {
        public LogView()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                // Подписки на изменения модели
                this.WhenAnyValue(x => x.ViewModel)
                    .Where(vm => vm != null)
                    .Subscribe()
                    .DisposeWith(disposables);
            });
        }
    }
}
