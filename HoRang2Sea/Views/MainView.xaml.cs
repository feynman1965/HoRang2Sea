using DevExpress.Mvvm.UI;
using DevExpress.Utils.MVVM.Services;
using DevExpress.Xpf.Ribbon;
using HoRang2Sea.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HoRang2Sea.Views
{
    /// <summary>
    /// Interaction logic for View1.xaml
    /// </summary>
    public partial class MainView : UserControl
    {
        public MainView()
        {
            DataContext = App.Container.GetInstance<MainViewModel>();

            InitializeComponent();
        }

        private void myCustomControl_Loaded(object sender, RoutedEventArgs e)
        {
            //Thread.Sleep(3000);
            var vm = (MainViewModel)this.DataContext;
            vm.SplashScreenService.HideSplashScreen();
        }
    }
}
