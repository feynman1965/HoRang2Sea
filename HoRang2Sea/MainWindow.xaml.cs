using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HoRang2Sea
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ThemedWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;

            // 작은 화면 · 높은 배율(예: 1920×1080 · 150%)에서 아래 상태줄이 화면 밖으로 나가지 않게 작업 영역에 맞춘다(2026-10-05)
            var wa = SystemParameters.WorkArea;
            if (Height > wa.Height) Height = wa.Height;
            if (Width > wa.Width) Width = wa.Width;
            MinWidth = Math.Min(800, wa.Width);
            MinHeight = Math.Min(560, wa.Height);
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 실행 중인 계산이 있으면 묻는다(이전에는 확인 없이 종료돼 내보내지 않은 결과가 사라졌다, 2026-10-05)
            try
            {
                var mv = App.Container?.GetInstance<HoRang2Sea.ViewModels.MainViewModel>();
                int running = mv?.Workspaces.OfType<HoRang2Sea.ViewModels.DocumentViewModel>().Count(d => d.IsSimulationActive) ?? 0;
                if (running > 0 && MessageBox.Show(this, $"{running} simulation(s) still running.\n\nExit anyway? Results that were not exported will be lost.",
                        "Exit", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }
            catch { }

            try
            {
                Environment.Exit(0);
            }
            catch { }
        }
    }
}
