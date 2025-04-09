using System;
using System.Linq;
using ReactiveUI;
using System.Reactive.Disposables;
using Syntec.TestApp.WPF.ViewModels;
using System.Reactive.Linq;

namespace Syntec.TestApp.WPF.Views
{
    /// <summary>
    /// Interaction logic for CncInfoView.xaml
    /// </summary>
    public partial class CncInfoView : ReactiveUserControl<CncInfo>
    {
        public CncInfoView()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                // Подписки на изменения модели
                this.WhenAnyValue(x => x.ViewModel)
                    .Where(vm => vm != null)
                    .Subscribe(vm => vm.UpdateInternalState())
                    .DisposeWith(disposables);
            });
        }
    }
}
