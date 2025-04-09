using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using MahApps.Metro.Controls;
using ReactiveUI;
using Syntec.TestApp.WPF.ViewModels;

namespace Syntec.TestApp.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, IViewFor<MainViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                // Подписки на изменения модели
                this.WhenAnyValue(x => x.ViewModel)
                    .Where(vm => vm != null)
                    .Subscribe(vm => DataContext = vm)
                    .DisposeWith(disposables);
            });
        }

        // Реализация IViewFor<MainViewModel>
        public MainViewModel ViewModel
        {
            get => (MainViewModel)DataContext;
            set => DataContext = value;
        }

        object IViewFor.ViewModel
        {
            get => DataContext;
            set => DataContext = (MainViewModel)value;
        }
    }
}
