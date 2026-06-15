using DevExpress.Mvvm.UI;
using DevExpress.Utils.MVVM.Services;
using DevExpress.Xpf.Ribbon;
using DevExpress.Xpf.Bars;
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
using System.Windows.Media.Effects;
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

        // ── Help: 순차 설명서 말풍선 가이드 ──────────────────────────────
        private static readonly (string Title, string Body)[] HelpSteps = new[]
        {
            ("1. Select a model",
             "Open a model from the Start page or the Solution Explorer on the left. Each model opens in its own document tab."),
            ("2. Set the inputs",
             "In Input mode, check the component diagram and load the input profile with the Load button. Edit any input values in the grid if needed."),
            ("3. Run the simulation",
             "Use Run, Pause, Step Forward and Stop on the ribbon to control the run. Values you edit in the input grid are applied to the model."),
            ("4. See the results",
             "Use the Input / Graph toggle to switch to the charts. The Graph view has y-t (time) and x-y tabs; pick the variables you want from the component tree and each one appears as its own tab."),
            ("5. Active layout",
             "The coloured pill on the bottom status bar shows the current Design / Control layout. Click it (or Select Layout on the ribbon) to switch configurations — the pill pulses when it changes."),
            ("6. Export results",
             "Use Export CSV to save the recorded simulation results to a file. The exported values match exactly what is shown on screen."),
        };

        private Window _helpGuideWindow;

        private void bHelp_ItemClick(object sender, ItemClickEventArgs e)
        {
            ShowHelpGuide();
        }

        private void ShowHelpGuide()
        {
            if (_helpGuideWindow != null) { _helpGuideWindow.Activate(); return; }

            int idx = 0;
            var accentBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x6D, 0xB4));

            var titleBlock = new TextBlock
            {
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            var bodyBlock = new TextBlock
            {
                FontSize = 13,
                LineHeight = 20,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2B, 0x2B, 0x2B)),
                TextWrapping = TextWrapping.Wrap
            };
            var counter = new TextBlock
            {
                FontSize = 12,
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center
            };

            var prevBtn = new Button { Content = "◀ Prev", Padding = new Thickness(12, 3, 12, 3), Margin = new Thickness(0, 0, 6, 0), MinWidth = 66 };
            var nextBtn = new Button { Content = "Next ▶", Padding = new Thickness(12, 3, 12, 3), MinWidth = 66 };

            var header = new Border
            {
                Background = accentBrush,
                CornerRadius = new CornerRadius(9, 9, 0, 0),
                Padding = new Thickness(16, 10, 16, 10),
                Child = titleBlock
            };

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            btnPanel.Children.Add(prevBtn);
            btnPanel.Children.Add(nextBtn);
            var footer = new DockPanel { Margin = new Thickness(0, 14, 0, 0) };
            DockPanel.SetDock(counter, Dock.Left);
            footer.Children.Add(counter);
            footer.Children.Add(btnPanel);

            var bodyPanel = new StackPanel { Margin = new Thickness(16, 14, 16, 14) };
            bodyPanel.Children.Add(bodyBlock);
            bodyPanel.Children.Add(footer);

            var card = new Grid();
            card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(header, 0);
            Grid.SetRow(bodyPanel, 1);
            card.Children.Add(header);
            card.Children.Add(bodyPanel);

            var bubble = new Border
            {
                Width = 380,
                Background = Brushes.White,
                BorderBrush = accentBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Child = card,
                Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 3, Opacity = 0.30, Color = Colors.Black }
            };

            // 말풍선 꼬리(위쪽 = 리본 Help 버튼 방향)
            var pointer = new System.Windows.Shapes.Polygon
            {
                Points = new PointCollection { new Point(0, 12), new Point(12, 0), new Point(24, 12) },
                Fill = accentBrush,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 40, -1)
            };

            var root = new StackPanel { Margin = new Thickness(16) };
            root.Children.Add(pointer);
            root.Children.Add(bubble);

            var win = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                SizeToContent = SizeToContent.WidthAndHeight,
                ShowInTaskbar = false,
                Topmost = true,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Owner = Window.GetWindow(this),
                Content = root
            };

            Action render = () =>
            {
                titleBlock.Text = HelpSteps[idx].Title;
                bodyBlock.Text = HelpSteps[idx].Body;
                counter.Text = (idx + 1) + " / " + HelpSteps.Length;
                prevBtn.IsEnabled = idx > 0;
                nextBtn.Content = idx == HelpSteps.Length - 1 ? "Done" : "Next ▶";
            };
            prevBtn.Click += (s, a) => { if (idx > 0) { idx--; render(); } };
            nextBtn.Click += (s, a) => { if (idx < HelpSteps.Length - 1) { idx++; render(); } else win.Close(); };
            win.KeyDown += (s, a) =>
            {
                if (a.Key == Key.Escape) win.Close();
                else if (a.Key == Key.Right) { if (idx < HelpSteps.Length - 1) { idx++; render(); } else win.Close(); }
                else if (a.Key == Key.Left) { if (idx > 0) { idx--; render(); } }
            };
            win.Closed += (s, a) => _helpGuideWindow = null;
            win.Loaded += (s, a) =>
            {
                var owner = win.Owner;
                if (owner != null && owner.ActualWidth > 0)
                {
                    win.Left = owner.Left + owner.ActualWidth - win.ActualWidth - 24;
                    win.Top = owner.Top + 88;
                }
            };

            render();
            _helpGuideWindow = win;
            win.Show();
        }
    }
}
