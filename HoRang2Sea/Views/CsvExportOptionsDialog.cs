using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HoRang2Sea.Views
{
    /// <summary>
    /// CSV export options dialog: 변수 선택 + 샘플링 (step/시간 토글) + 시간 범위.
    /// </summary>
    public class CsvExportOptionsDialog : Window
    {
        private static readonly Brush SkyBlue = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7));   // Light Sky Blue
        private static readonly Brush SkyBlueDeep = new SolidColorBrush(Color.FromRgb(0x29, 0xB6, 0xF6));// Sky Blue
        private static readonly Brush BorderGray = new SolidColorBrush(Color.FromRgb(0xBD, 0xBD, 0xBD));

        private readonly TextBox _stepIntervalBox;
        private readonly TextBox _startStepBox;
        private readonly TextBox _endStepBox;
        private readonly TextBox _stepsPerSecondBox;
        private readonly CheckBox _modeStepCheck;
        private readonly CheckBox _modeTimeCheck;
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

            // Sky-blue square checkbox style 등록 (다이얼로그 내 모든 CheckBox에 적용)
            Resources.Add(typeof(CheckBox), MakeSkyBlueCheckBoxStyle());

            var root = new Grid { Margin = new Thickness(15) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

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

            // ---- 2: 샘플링 방식 (CheckBox 2개, 양자택일 보장 + 둘 다 해제 시 전체 저장) ----
            var sampleBox = new GroupBox { Header = "샘플링 방식 (둘 다 해제 시 전체 저장)", Margin = new Thickness(0, 0, 0, 8) };
            var sampleGrid = new Grid();
            sampleGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            sampleGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Step 기준
            _modeStepCheck = new CheckBox
            {
                Content = "Step 기준 — N step마다 저장",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(4, 6, 4, 2)
            };
            var stepInner = MakeFormGrid();
            AddFormRow(stepInner, 0, "Step 간격 (1=전체, 10=10step)", out _stepIntervalBox, "1");
            stepInner.Margin = new Thickness(24, 0, 4, 6);
            stepInner.SetBinding(IsEnabledProperty, new Binding("IsChecked") { Source = _modeStepCheck });
            var stepStack = new StackPanel();
            stepStack.Children.Add(_modeStepCheck);
            stepStack.Children.Add(stepInner);
            Grid.SetRow(stepStack, 0);
            sampleGrid.Children.Add(stepStack);

            // 시간 기준
            _modeTimeCheck = new CheckBox
            {
                Content = "시간 기준 — 1초 간격으로 저장",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(4, 6, 4, 2)
            };
            var timeInner = MakeFormGrid();
            AddFormRow(timeInner, 0, "Steps per second (DLL 보통 100)", out _stepsPerSecondBox, "100");
            timeInner.Margin = new Thickness(24, 0, 4, 6);
            timeInner.SetBinding(IsEnabledProperty, new Binding("IsChecked") { Source = _modeTimeCheck });
            var timeStack = new StackPanel();
            timeStack.Children.Add(_modeTimeCheck);
            timeStack.Children.Add(timeInner);
            Grid.SetRow(timeStack, 1);
            sampleGrid.Children.Add(timeStack);

            // 양자택일: 한쪽 켜면 다른쪽 자동 해제 (둘 다 해제는 허용)
            _modeStepCheck.Checked += (s, e) => { _modeTimeCheck.IsChecked = false; };
            _modeTimeCheck.Checked += (s, e) => { _modeStepCheck.IsChecked = false; };

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

        // 사각형 + 하늘색 체크 표시
        private static Style MakeSkyBlueCheckBoxStyle()
        {
            var style = new Style(typeof(CheckBox));
            style.Setters.Add(new Setter(System.Windows.Controls.Control.PaddingProperty, new Thickness(6, 0, 0, 0)));

            var template = new ControlTemplate(typeof(CheckBox));
            // Root grid: [16x16 box] [content]
            var grid = new FrameworkElementFactory(typeof(Grid));
            grid.SetValue(FrameworkElement.SnapsToDevicePixelsProperty, true);
            grid.SetValue(System.Windows.Controls.Control.BackgroundProperty, Brushes.Transparent);

            var col0 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col0.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            grid.AppendChild(col0);
            grid.AppendChild(col1);

            // Box (Border)
            var border = new FrameworkElementFactory(typeof(Border), "PART_Box");
            border.SetValue(FrameworkElement.WidthProperty, 16d);
            border.SetValue(FrameworkElement.HeightProperty, 16d);
            border.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.SetValue(Border.BackgroundProperty, Brushes.White);
            border.SetValue(Border.BorderBrushProperty, BorderGray);
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1.5));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));

            // 체크 표시 (Path) — 사각형 내부 V 모양 흰색 (켜졌을 때 보임)
            var checkPath = new FrameworkElementFactory(typeof(Path), "PART_Check");
            checkPath.SetValue(Path.DataProperty, Geometry.Parse("M 2,5 L 5.5,8.5 L 11,3"));
            checkPath.SetValue(Shape.StrokeProperty, Brushes.White);
            checkPath.SetValue(Shape.StrokeThicknessProperty, 2.0);
            checkPath.SetValue(Shape.StrokeStartLineCapProperty, PenLineCap.Round);
            checkPath.SetValue(Shape.StrokeEndLineCapProperty, PenLineCap.Round);
            checkPath.SetValue(Shape.StrokeLineJoinProperty, PenLineJoin.Round);
            checkPath.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            checkPath.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            checkPath.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            border.AppendChild(checkPath);

            grid.AppendChild(border);

            // Content
            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(Grid.ColumnProperty, 1);
            content.SetValue(FrameworkElement.MarginProperty, new Thickness(6, 0, 0, 0));
            content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            grid.AppendChild(content);

            template.VisualTree = grid;

            // Triggers
            var checkedTrigger = new Trigger { Property = System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, Value = true };
            checkedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, SkyBlueDeep, "PART_Box"));
            checkedTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, SkyBlueDeep, "PART_Box"));
            checkedTrigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "PART_Check"));
            template.Triggers.Add(checkedTrigger);

            var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, SkyBlue, "PART_Box"));
            template.Triggers.Add(hoverTrigger);

            var disabledTrigger = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.5));
            template.Triggers.Add(disabledTrigger);

            style.Setters.Add(new Setter(System.Windows.Controls.Control.TemplateProperty, template));
            return style;
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

            if (_modeTimeCheck.IsChecked == true)
            {
                if (!int.TryParse(_stepsPerSecondBox.Text, out int sps) || sps < 1)
                {
                    MessageBox.Show("Steps per second는 1 이상의 정수여야 합니다.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                interval = sps;
            }
            else if (_modeStepCheck.IsChecked == true)
            {
                if (!string.IsNullOrWhiteSpace(_stepIntervalBox.Text) && !int.TryParse(_stepIntervalBox.Text, out interval))
                {
                    MessageBox.Show("Step 간격은 정수여야 합니다.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                if (interval < 1) interval = 1;
            }
            // else: 둘 다 해제 → interval = 1 (전체 저장)

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
