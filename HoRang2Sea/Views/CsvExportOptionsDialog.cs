using System.Windows;
using System.Windows.Controls;

namespace HoRang2Sea.Views
{
    /// <summary>
    /// Simple CSV export options dialog (sampling + time range).
    /// Built in code (no XAML) to keep integration light.
    /// </summary>
    public class CsvExportOptionsDialog : Window
    {
        private readonly TextBox _stepIntervalBox;
        private readonly TextBox _startStepBox;
        private readonly TextBox _endStepBox;
        private readonly CheckBox _useTimeBased;
        private readonly TextBox _stepsPerSecondBox;

        public int StepInterval { get; private set; } = 1;
        public int StartStep { get; private set; } = -1;
        public int EndStep { get; private set; } = -1;

        public CsvExportOptionsDialog(int totalSteps)
        {
            Title = "CSV 저장 옵션";
            Width = 420;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid { Margin = new Thickness(15) };
            for (int i = 0; i < 8; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var totalLabel = new TextBlock
            {
                Text = $"기록된 총 step 수: {totalSteps}",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(totalLabel, 0); Grid.SetColumnSpan(totalLabel, 2);
            grid.Children.Add(totalLabel);

            AddRow(grid, 1, "Step 간격 (1=전체, 10=10step마다)", out _stepIntervalBox, "1");
            AddRow(grid, 2, "시작 step (비우면 처음부터)", out _startStepBox, "");
            AddRow(grid, 3, "종료 step (비우면 끝까지)", out _endStepBox, "");

            _useTimeBased = new CheckBox
            {
                Content = "시간 기준 저장 (1초 간격으로 변환)",
                Margin = new Thickness(0, 10, 0, 5)
            };
            Grid.SetRow(_useTimeBased, 4); Grid.SetColumnSpan(_useTimeBased, 2);
            grid.Children.Add(_useTimeBased);

            AddRow(grid, 5, "Steps per second (DLL 기준 보통 100)", out _stepsPerSecondBox, "100");

            var hint = new TextBlock
            {
                Text = "* 시간 기준 사용 시 'Step 간격'은 자동 계산됩니다.\n* 종료 step 비우면 마지막까지 저장.",
                Foreground = System.Windows.Media.Brushes.Gray,
                FontSize = 11,
                Margin = new Thickness(0, 8, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(hint, 6); Grid.SetColumnSpan(hint, 2);
            grid.Children.Add(hint);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0)
            };
            var okBtn = new Button { Content = "저장", Width = 80, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            var cancelBtn = new Button { Content = "취소", Width = 80, IsCancel = true };
            okBtn.Click += (s, e) =>
            {
                if (TryParseValues())
                {
                    DialogResult = true;
                    Close();
                }
            };
            btnPanel.Children.Add(okBtn);
            btnPanel.Children.Add(cancelBtn);
            Grid.SetRow(btnPanel, 7); Grid.SetColumnSpan(btnPanel, 2);
            grid.Children.Add(btnPanel);

            Content = grid;
        }

        private static void AddRow(Grid grid, int row, string label, out TextBox box, string defaultValue)
        {
            var lbl = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 4, 8, 4) };
            Grid.SetRow(lbl, row); Grid.SetColumn(lbl, 0);
            grid.Children.Add(lbl);

            box = new TextBox { Text = defaultValue, Margin = new Thickness(0, 4, 0, 4), Padding = new Thickness(4, 2, 4, 2) };
            Grid.SetRow(box, row); Grid.SetColumn(box, 1);
            grid.Children.Add(box);
        }

        private bool TryParseValues()
        {
            int interval = 1;
            if (!string.IsNullOrWhiteSpace(_stepIntervalBox.Text) && !int.TryParse(_stepIntervalBox.Text, out interval))
            {
                MessageBox.Show("Step 간격은 정수여야 합니다.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (interval < 1) interval = 1;

            int startStep = -1;
            if (!string.IsNullOrWhiteSpace(_startStepBox.Text) && !int.TryParse(_startStepBox.Text, out startStep))
            {
                MessageBox.Show("시작 step은 정수여야 합니다.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            int endStep = -1;
            if (!string.IsNullOrWhiteSpace(_endStepBox.Text) && !int.TryParse(_endStepBox.Text, out endStep))
            {
                MessageBox.Show("종료 step은 정수여야 합니다.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (_useTimeBased.IsChecked == true)
            {
                if (!int.TryParse(_stepsPerSecondBox.Text, out int sps) || sps < 1)
                {
                    MessageBox.Show("Steps per second는 1 이상의 정수여야 합니다.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                interval = sps; // 1초당 1개 sample
            }

            StepInterval = interval;
            StartStep = startStep;
            EndStep = endStep;
            return true;
        }
    }
}
