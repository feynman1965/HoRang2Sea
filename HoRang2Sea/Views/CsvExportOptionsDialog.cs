using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HoRang2Sea.Views
{
    /// <summary>
    /// CSV export options dialog: 변수 선택 + 샘플링 (step/시간 간격) + 시간 범위.
    /// 코드로 빌드 (XAML 없음) — 통합 가벼움.
    /// </summary>
    public class CsvExportOptionsDialog : Window
    {
        private readonly TextBox _stepIntervalBox;
        private readonly TextBox _startStepBox;
        private readonly TextBox _endStepBox;
        private readonly CheckBox _useTimeBased;
        private readonly TextBox _stepsPerSecondBox;
        private readonly List<CheckBox> _varCheckBoxes = new();

        public int StepInterval { get; private set; } = 1;
        public int StartStep { get; private set; } = -1;
        public int EndStep { get; private set; } = -1;
        public List<string> SelectedVariables { get; private set; } = new();

        public CsvExportOptionsDialog(int totalSteps) : this(totalSteps, null) { }

        public CsvExportOptionsDialog(int totalSteps, IList<string> availableHeaders)
        {
            Title = "CSV 저장 옵션";
            Width = 460;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResize;

            var root = new Grid { Margin = new Thickness(15) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 변수 선택 영역
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ---- 상단: 총 step + 샘플링/범위 ----
            var topGrid = new Grid();
            for (int i = 0; i < 6; i++) topGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var totalLabel = new TextBlock
            {
                Text = $"기록된 총 step 수: {totalSteps}",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(totalLabel, 0); Grid.SetColumnSpan(totalLabel, 2);
            topGrid.Children.Add(totalLabel);

            AddRow(topGrid, 1, "Step 간격 (1=전체, 10=10step마다)", out _stepIntervalBox, "1");
            AddRow(topGrid, 2, "시작 step (비우면 처음부터)", out _startStepBox, "");
            AddRow(topGrid, 3, "종료 step (비우면 끝까지)", out _endStepBox, "");

            _useTimeBased = new CheckBox
            {
                Content = "시간 기준 저장 (1초 간격으로 변환)",
                Margin = new Thickness(0, 8, 0, 4)
            };
            Grid.SetRow(_useTimeBased, 4); Grid.SetColumnSpan(_useTimeBased, 2);
            topGrid.Children.Add(_useTimeBased);

            AddRow(topGrid, 5, "Steps per second (DLL 기준 보통 100)", out _stepsPerSecondBox, "100");
            Grid.SetRow(topGrid, 0);
            root.Children.Add(topGrid);

            // ---- 중단: 변수 선택 ----
            var varSection = new GroupBox
            {
                Header = "저장 변수 선택 (전부 끄면 전체 저장)",
                Margin = new Thickness(0, 12, 0, 0)
            };
            var varGrid = new Grid();
            varGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            varGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4) };
            var allBtn = new Button { Content = "Select All", Width = 90, Margin = new Thickness(0, 0, 6, 0) };
            var noneBtn = new Button { Content = "Clear", Width = 60 };
            allBtn.Click += (s, e) => { foreach (var cb in _varCheckBoxes) cb.IsChecked = true; };
            noneBtn.Click += (s, e) => { foreach (var cb in _varCheckBoxes) cb.IsChecked = false; };
            btnRow.Children.Add(allBtn);
            btnRow.Children.Add(noneBtn);
            Grid.SetRow(btnRow, 0);
            varGrid.Children.Add(btnRow);

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var varStack = new StackPanel { Margin = new Thickness(8, 0, 8, 4) };
            if (availableHeaders != null)
            {
                // Step 컬럼은 항상 포함 (선택 UI에서 제외)
                foreach (var h in availableHeaders.Skip(1))
                {
                    var cb = new CheckBox { Content = h, IsChecked = true, Margin = new Thickness(2) };
                    _varCheckBoxes.Add(cb);
                    varStack.Children.Add(cb);
                }
            }
            scroll.Content = varStack;
            Grid.SetRow(scroll, 1);
            varGrid.Children.Add(scroll);
            varSection.Content = varGrid;
            Grid.SetRow(varSection, 1);
            root.Children.Add(varSection);

            // ---- 안내 ----
            var hint = new TextBlock
            {
                Text = "* 시간 기준 사용 시 'Step 간격'은 자동 계산됩니다.\n* 종료 step 비우면 마지막까지.\n* 변수 모두 체크 해제 시 전체 변수 저장.",
                Foreground = System.Windows.Media.Brushes.Gray,
                FontSize = 11,
                Margin = new Thickness(0, 8, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(hint, 2);
            root.Children.Add(hint);

            // ---- 버튼 ----
            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
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
            Grid.SetRow(btnPanel, 3);
            root.Children.Add(btnPanel);

            Content = root;
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
                interval = sps;
            }

            StepInterval = interval;
            StartStep = startStep;
            EndStep = endStep;

            // 변수 선택: 모두 체크돼 있거나 모두 해제된 경우 → 빈 리스트 (전체 저장)
            var checkedVars = _varCheckBoxes.Where(cb => cb.IsChecked == true).Select(cb => cb.Content?.ToString() ?? "").ToList();
            if (checkedVars.Count == 0 || checkedVars.Count == _varCheckBoxes.Count)
                SelectedVariables = new List<string>();
            else
                SelectedVariables = checkedVars;

            return true;
        }
    }
}
