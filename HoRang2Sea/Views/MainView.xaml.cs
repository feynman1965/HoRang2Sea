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
            Current = this;   // 시작 화면 HELP 가 도움말을 띄운다(2026-10-06)
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
            ("1. Open a model",
             "Click Menu (top left) and choose New, then pick a model. Saved and History reopen configurations you saved or ran before. Each model opens in its own tab."),
            ("2. Set the inputs",
             "Pick the layout in the window that opens (or click the coloured pill on the bottom status bar). Load a speed profile (ratio 0 to 1) with Load Profile; without one, the model's default profile is used. Edit input values in the grid if needed - keep them within Min and Max."),
            ("3. Run the simulation",
             "Run starts or resumes the run, Pause holds it, Step Forward advances one step (1 ms) while paused, and Stop ends it - the results stay on screen until the next Run. Inputs changed during a run apply from the next Run."),
            ("4. See the results",
             "Switch to Graph and choose y-t, y-t (multi) or y-x. Click the first button on the left of the chart (Select Variables) and double-click outputs to add them."),
            ("5. Active layout",
             "The coloured pill on the bottom status bar shows the current layout. Click it to switch; if a simulation is running you are asked first."),
            ("6. Export results",
             "Export CSV saves the recorded results (Step, Time_s and the outputs you choose). Each Run also saves the input values sent to the model in %LOCALAPPDATA%\\HoRang2\\RunInputs\\<model>."),
        };

        private Window _helpGuideWindow;

        private void bHelp_ItemClick(object sender, ItemClickEventArgs e)
        {
            ShowHelpGuide();
        }

        internal static MainView Current { get; private set; }
        internal void ShowHelp() => ShowHelpGuide();

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
                if (owner == null) return;

                // Help 버튼 실제 위치에 말풍선 꼬리를 맞춤 (못 찾으면 우상단 폴백)
                var btn = FindBarItemVisual(owner, bHelp);
                if (btn != null)
                {
                    var src = PresentationSource.FromVisual(owner);
                    double sx = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
                    double sy = src?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
                    var devPt = btn.PointToScreen(new Point(btn.ActualWidth / 2.0, btn.ActualHeight));
                    double btnCenterX = devPt.X / sx;
                    double btnBottomY = devPt.Y / sy;
                    // 꼬리 중심 = win.Left + 344, 꼬리 끝(tip) = win.Top + 16
                    win.Left = btnCenterX - 344;
                    win.Top = btnBottomY - 14;
                }
                else if (owner.ActualWidth > 0)
                {
                    win.Left = owner.Left + owner.ActualWidth - win.ActualWidth - 24;
                    win.Top = owner.Top + 88;
                }

                if (win.Left < owner.Left + 8) win.Left = owner.Left + 8;
            };

            render();
            _helpGuideWindow = win;
            win.Show();
        }

        // Help 버튼의 렌더된 비주얼(링크 컨트롤)을 찾아 화면 좌표 계산에 사용 (가장 넓은 매칭 = 버튼 본체)
        private static FrameworkElement FindBarItemVisual(DependencyObject root, object barItem)
        {
            FrameworkElement best = null;
            void Walk(DependencyObject d)
            {
                if (d is FrameworkElement fe && fe.DataContext == barItem && fe.IsVisible
                    && fe.ActualWidth > 0 && fe.ActualHeight > 0)
                {
                    if (best == null || fe.ActualWidth > best.ActualWidth) best = fe;
                }
                int n = VisualTreeHelper.GetChildrenCount(d);
                for (int i = 0; i < n; i++) Walk(VisualTreeHelper.GetChild(d, i));
            }
            Walk(root);
            return best;
        }
    }
}
