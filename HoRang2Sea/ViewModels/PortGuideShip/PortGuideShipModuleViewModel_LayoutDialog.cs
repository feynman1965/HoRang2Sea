using HoRang2Sea.Models;
using System.Linq;

namespace HoRang2Sea.ViewModels
{
    public partial class PortGuideShipModuleViewModel
    {
        private bool _layoutDialogOpen = false;
        public void ShowLayoutSelectionDialog()
        {
            if (_layoutDialogOpen) return;
            _layoutDialogOpen = true;
            try { ShowLayoutSelectionDialogInternal(); }
            finally { _layoutDialogOpen = false; }
        }

        public override void OpenItemByItem(SolutionItem item)
        {
            ShowLayoutSelectionDialog();
            base.OpenItemByItem(item);
        }

        // 레이아웃 선택 다이얼로그 표시
        private void ShowLayoutSelectionDialogInternal()
        {
            var dialog = new System.Windows.Window
            {
                Title = "Select Layout - PortGuideShip",
                Width = 700,
                Height = 680,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                WindowStyle = System.Windows.WindowStyle.SingleBorderWindow,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 247, 250))
            };

            var mainGrid = new System.Windows.Controls.Grid();
            mainGrid.Margin = new System.Windows.Thickness(35);

            // 제목 영역
            var headerStack = new System.Windows.Controls.StackPanel
            {
                Margin = new System.Windows.Thickness(0, 0, 0, 25)
            };

            var title = new System.Windows.Controls.TextBlock
            {
                Text = "Layout Configuration",
                FontSize = 26,
                FontWeight = System.Windows.FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 73, 94))
            };

            var subtitle = new System.Windows.Controls.TextBlock
            {
                Text = "Choose your preferred system design and control configuration",
                FontSize = 13,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new System.Windows.Thickness(0, 8, 0, 0),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166))
            };

            var divider = new System.Windows.Controls.Border
            {
                Height = 2,
                Background = new System.Windows.Media.LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0.5),
                    EndPoint = new System.Windows.Point(1, 0.5),
                    GradientStops = new System.Windows.Media.GradientStopCollection
                    {
                        new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromArgb(0, 52, 152, 219), 0),
                        new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromArgb(255, 52, 152, 219), 0.5),
                        new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromArgb(0, 52, 152, 219), 1)
                    }
                },
                Margin = new System.Windows.Thickness(0, 12, 0, 0)
            };

            headerStack.Children.Add(title);
            headerStack.Children.Add(subtitle);
            headerStack.Children.Add(divider);

            // 2x2 버튼 그리드
            var buttonGrid = new System.Windows.Controls.Grid
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            buttonGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            buttonGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });

            // 버튼 스타일 정의 함수
            System.Windows.Controls.Button CreateLayoutButton(string text, string description, int designValue, int controlValue, System.Windows.Media.Color accentColor)
            {
                var button = new System.Windows.Controls.Button
                {
                    Width = 280,
                    Height = 150,
                    Margin = new System.Windows.Thickness(12),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 225, 230)),
                    BorderThickness = new System.Windows.Thickness(2),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = System.Windows.Media.Colors.Gray,
                        Direction = 320,
                        ShadowDepth = 4,
                        BlurRadius = 10,
                        Opacity = 0.25
                    }
                };

                var stack = new System.Windows.Controls.StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Vertical,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center
                };

                var titleText = new System.Windows.Controls.TextBlock
                {
                    Text = text,
                    FontSize = 18,
                    FontWeight = System.Windows.FontWeights.Bold,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 62, 80))
                };

                var descText = new System.Windows.Controls.TextBlock
                {
                    Text = description,
                    FontSize = 12,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(127, 140, 141)),
                    Margin = new System.Windows.Thickness(0, 6, 0, 0),
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    TextAlignment = System.Windows.TextAlignment.Center
                };

                stack.Children.Add(titleText);
                stack.Children.Add(descText);
                button.Content = stack;

                // 마우스 오버 효과
                button.MouseEnter += (s, e) =>
                {
                    button.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 251, 253));
                    button.BorderBrush = new System.Windows.Media.SolidColorBrush(accentColor);
                    button.BorderThickness = new System.Windows.Thickness(3);
                    var effect = button.Effect as System.Windows.Media.Effects.DropShadowEffect;
                    if (effect != null)
                    {
                        effect.Color = accentColor;
                        effect.BlurRadius = 15;
                        effect.Opacity = 0.4;
                    }
                };

                button.MouseLeave += (s, e) =>
                {
                    button.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                    button.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 225, 230));
                    button.BorderThickness = new System.Windows.Thickness(2);
                    var effect = button.Effect as System.Windows.Media.Effects.DropShadowEffect;
                    if (effect != null)
                    {
                        effect.Color = System.Windows.Media.Colors.Gray;
                        effect.BlurRadius = 10;
                        effect.Opacity = 0.25;
                    }
                };

                button.Click += (s, e) =>
                {
                    // 시뮬레이션 실행 중이거나 일시정지 상태면 먼저 정지
                    if (BaseMWModel is PortGuideShipMW mw && mw.CalculateThread != null && (mw.CalculateThread.IsAlive || mw.IsPause))
                    {
                        mw.StopCalculation();
                    }

                    DesignLayout = designValue;
                    ControlLayout = controlValue;
                    SyncLayoutToDatabase();
                    UpdateLayoutVisibility();
                    dialog.Close();
                };

                return button;
            }

            // 4개의 레이아웃 버튼 생성 (2x2 배치) - 각기 다른 색상
            var btn1 = CreateLayoutButton(
                "Default Layout",
                "Standard configuration\nDesign: 0 | Control: 0",
                0, 0,
                System.Windows.Media.Color.FromRgb(52, 152, 219)); // Blue
            System.Windows.Controls.Grid.SetRow(btn1, 0);
            System.Windows.Controls.Grid.SetColumn(btn1, 0);

            var btn2 = CreateLayoutButton(
                "Control Mode",
                "Enhanced control settings\nDesign: 0 | Control: 1",
                0, 1,
                System.Windows.Media.Color.FromRgb(46, 204, 113)); // Green
            System.Windows.Controls.Grid.SetRow(btn2, 0);
            System.Windows.Controls.Grid.SetColumn(btn2, 1);

            var btn3 = CreateLayoutButton(
                "Design Mode",
                "Advanced design features\nDesign: 1 | Control: 0",
                1, 0,
                System.Windows.Media.Color.FromRgb(155, 89, 182)); // Purple
            System.Windows.Controls.Grid.SetRow(btn3, 1);
            System.Windows.Controls.Grid.SetColumn(btn3, 0);

            var btn4 = CreateLayoutButton(
                "Full Configuration",
                "Complete system setup\nDesign: 1 | Control: 1",
                1, 1,
                System.Windows.Media.Color.FromRgb(230, 126, 34)); // Orange
            System.Windows.Controls.Grid.SetRow(btn4, 1);
            System.Windows.Controls.Grid.SetColumn(btn4, 1);

            buttonGrid.Children.Add(btn1);
            buttonGrid.Children.Add(btn2);
            buttonGrid.Children.Add(btn3);
            buttonGrid.Children.Add(btn4);

            var container = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Vertical
            };
            container.Children.Add(headerStack);
            container.Children.Add(buttonGrid);

            mainGrid.Children.Add(container);
            dialog.Content = mainGrid;

            dialog.ShowDialog();
        }

        private void SyncLayoutToDatabase()
        {
            if (Database == null || Database.Tables == null) return;
            var layoutTable = Database.Tables.FirstOrDefault(t => t.Name == "Layout");
            if (layoutTable == null) return;
            var modeCol = layoutTable.Columns.FirstOrDefault(c => c.Name == "Mode");
            var designCol = layoutTable.Columns.FirstOrDefault(c => c.Name == "Design_Layout");
            var controlCol = layoutTable.Columns.FirstOrDefault(c => c.Name == "Control_Layout");
            if (modeCol != null) modeCol.Init = "1";
            if (designCol != null) designCol.Init = DesignLayout.ToString();
            if (controlCol != null) controlCol.Init = ControlLayout.ToString();
        }
    }
}
