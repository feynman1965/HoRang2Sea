using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace HoRang2Sea.Views
{
    /// <summary>
    /// CSV export options dialog: 변수 선택 + 샘플링 (step/시간 토글) + 시간 범위.
    /// </summary>
    public class CsvExportOptionsDialog : Window
    {
        private readonly TextBox _stepIntervalBox;
        private readonly TextBox _startStepBox;
        private readonly TextBox _endStepBox;
        private readonly TextBox _stepsPerSecondBox;
        private readonly RadioButton _modeStepRadio;
        private readonly RadioButton _modeTimeRadio;
        private readonly List<CheckBox> _varCheckBoxes = new();

        public int StepInterval { get; private set; } = 1;
        public int StartStep { get; private set; } = -1;
        public int EndStep { get; private set; } = -1;
        public List<string> SelectedVariables { get; private set; } = new();

        public CsvExportOptionsDialog(int totalSteps) : this(totalSteps, null) { }

        public CsvExportOptionsDialog(int totalSteps, IList<string> availableHeaders)
        {
            Title = "CSV 저장 옵션";
            Width = 500;
            Height = 660;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResize;
            MinWidth = 420;
            MinHeight = 520;

            var root = new Grid { Margin = new Thickness(15) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 총 step
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 시간 범위
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 샘플링 방식
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 변수 선택
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // hint
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 버튼

            // ---- 0: 총 step ----
            var totalLabel = new TextBlock
            {
                Text = $"기록된 총 step 수: {totalSteps}",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(totalLabel, 0);
            root.Children.Add(totalLabel);

            // ---- 1: 시간 범위 ----
            var rangeBox = new GroupBox { Header = "시간 범위 (옵션)", Margin = new Thickness(0, 0, 0, 8) };
            var rangeGrid = MakeFormGrid();
            AddFormRow(rangeGrid, 0, "시작 step (비우면 처음부터)", out _startStepBox, "");
            AddFormRow(rangeGrid, 1, "종료 step (비우면 끝까지)", out _endStepBox, "");
            rangeBox.Content = rangeGrid;
            Grid.SetRow(rangeBox, 1);
            root.Children.Add(rangeBox);

            // ---- 2: 샘플링 방식 토글 ----
            var sampleBox = new GroupBox { Header = "샘플링 방식", Margin = new Thickness(0, 0, 0, 8) };
            var sampleGrid = new Grid();
            sampleGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            sampleGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Step 기준 라디오
            _modeStepRadio = new RadioButton
            {
                Content = "Step 기준 — N step마다 저장",
                IsChecked = true,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(4, 6, 4, 2)
            };
            var stepInner = MakeFormGrid();
            AddFormRow(stepInner, 0, "Step 간격 (1=전체, 10=10step)", out _stepIntervalBox, "1");
            stepInner.Margin = new Thickness(24, 0, 4, 6);
            stepInner.SetBinding(IsEnabledProperty, new Binding("IsChecked") { Source = _modeStepRadio });
            var stepStack = new StackPanel();
            stepStack.Children.Add(_modeStepRadio);
            stepStack.Children.Add(stepInner);
            Grid.SetRow(stepStack, 0);
            sampleGrid.Children.Add(stepStack);

            // 시간 기준 라디오
            _modeTimeRadio = new RadioButton
            {
                Content = "시간 기준 — 1초 간격으로 저장",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(4, 6, 4, 2)
            };
            var timeInner = MakeFormGrid();
            AddFormRow(timeInner, 0, "Steps per second (DLL 보통 100)", out _stepsPerSecondBox, "100");
            timeInner.Margin = new Thickness(24, 0, 4, 6);
            timeInner.SetBinding(IsEnabledProperty, new Binding("IsChecked") { Source = _modeTimeRadio });
            var timeStack = new StackPanel();
            timeStack.Children.Add(_modeTimeRadio);
            timeStack.Children.Add(timeInner);
            Grid.SetRow(timeStack, 1);
            sampleGrid.Children.Add(timeStack);

            sampleBox.Content = sampleGrid;
            Grid.SetRow(sampleBox, 2);
            root.Children.Add(sampleBox);

            // ---- 3: 변수 선택 ----
            var varSection = new GroupBox { Header = "저장 변수 선택 (전부 끄면 전체 저장)" };
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
                foreach (var h in availableHeaders.Skip(1)) // Step 컬럼 제외
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
            Grid.SetRow(varSection, 3);
            root.Children.Add(varSection);

            // ---- 4: hint ----
            var hint = new TextBlock
            {
                Text = "* 시간 범위 비우면 전체 step 저장. * 변수 모두 체크 해제 시 전체 변수 저장.",
                Foreground = System.Windows.Media.Brushes.Gray,
                FontSize = 11,
                Margin = new Thickness(0, 8, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(hint, 4);
            root.Children.Add(hint);

            // ---- 5: 버튼 ----
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
            Grid.SetRow(btnPanel, 5);
            root.Children.Add(btnPanel);

            Content = root;
        }

        private static Grid MakeFormGrid()
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            return g;
        }

        private static void AddFormRow(Grid grid, int row, string label, out TextBox box, string defaultValue)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var lbl = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 4, 8, 4),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(lbl, row); Grid.SetColumn(lbl, 0);
            grid.Children.Add(lbl);

            box = new TextBox
            {
                Text = defaultValue,
                Margin = new Thickness(0, 4, 4, 4),
                Padding = new Thickness(4, 2, 4, 2),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(box, row); Grid.SetColumn(box, 1);
            grid.Children.Add(box);
        }

        private bool TryParseValues()
        {
            int interval = 1;
            int startStep = -1;
            int endStep = -1;

            if (!string.IsNullOrWhiteSpace(_startStepBox.Text) && !int.TryParse(_startStepBox.Text, out startStep))
            {
                MessageBox.Show("시작 step은 정수여야 합니다.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!string.IsNullOrWhiteSpace(_endStepBox.Text) && !int.TryParse(_endStepBox.Text, out endStep))
            {
                MessageBox.Show("종료 step은 정수여야 합니다.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (_modeTimeRadio.IsChecked == true)
            {
                if (!int.TryParse(_stepsPerSecondBox.Text, out int sps) || sps < 1)
                {
                    MessageBox.Show("Steps per second는 1 이상의 정수여야 합니다.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                interval = sps; // 1초당 1 sample
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(_stepIntervalBox.Text) && !int.TryParse(_stepIntervalBox.Text, out interval))
                {
                    MessageBox.Show("Step 간격은 정수여야 합니다.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                if (interval < 1) interval = 1;
            }

            StepInterval = interval;
            StartStep = startStep;
            EndStep = endStep;

            var checkedVars = _varCheckBoxes.Where(cb => cb.IsChecked == true).Select(cb => cb.Content?.ToString() ?? "").ToList();
            SelectedVariables = (checkedVars.Count == 0 || checkedVars.Count == _varCheckBoxes.Count)
                ? new List<string>()
                : checkedVars;

            return true;
        }
    }
}
