﻿using System;
using System.Linq;
using ReactiveUI;
using System.Reactive.Disposables;
using Syntec.TestApp.WPF.ViewModels;
using System.Reactive.Linq;

namespace Syntec.TestApp.WPF.Views
{
    /// <summary>
    /// Interaction logic for CncStatusView.xaml
    /// </summary>
    public partial class CncStatusView : ReactiveUserControl<CncStatus>
    {
        public CncStatusView()
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
