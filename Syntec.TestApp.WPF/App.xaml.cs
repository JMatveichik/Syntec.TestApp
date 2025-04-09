using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Splat;
using ReactiveUI;

namespace Syntec.TestApp.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Инициализация ReactiveUI
            Locator.CurrentMutable.InitializeReactiveUI();
        }
    }
}
