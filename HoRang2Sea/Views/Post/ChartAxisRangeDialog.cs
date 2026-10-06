using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using SciChart.Charting.Model.ChartSeries;
using SciChart.Data.Model;

namespace HoRang2Sea.Views
{
    // 차트 축 범위를 키보드로 직접 입력하는 다이얼로그 (y-t / x-y 차트 공용).
    // Apply 시 해당 축 AutoRange=Never + VisibleRange 적용 → 수동 스케일 고정.
    internal static class ChartAxisRangeDialog
    {
        public static void Show(IEnumerable<IAxisViewModel> xAxes, IEnumerable<IAxisViewModel> yAxes)
        {
            var rows = new List<(NumericAxisViewModel axis, TextBox min, TextBox max)>();
            var panel = new StackPanel { Margin = new Thickness(16) };

            void AddAxisRow(NumericAxisViewModel ax, string fallbackName)
            {
                var name = string.IsNullOrEmpty(ax.AxisTitle) ? fallbackName : ax.AxisTitle;
                panel.Children.Add(new TextBlock
                {
                    Text = name,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 8, 0, 3),
                    Foreground = System.Windows.Media.Brushes.DarkSlateGray
                });
                var row = new StackPanel { Orientation = Orientation.Horizontal };
                var cur = ax.VisibleRange as DoubleRange;
                var minBox = new TextBox { Width = 120, Text = cur != null ? cur.Min.ToString("G6") : "", VerticalContentAlignment = VerticalAlignment.Center };
                var maxBox = new TextBox { Width = 120, Text = cur != null ? cur.Max.ToString("G6") : "", VerticalContentAlignment = VerticalAlignment.Center };
                row.Children.Add(new TextBlock { Text = "Min ", VerticalAlignment = VerticalAlignment.Center });
                row.Children.Add(minBox);
                row.Children.Add(new TextBlock { Text = "   Max ", VerticalAlignment = VerticalAlignment.Center });
                row.Children.Add(maxBox);
                panel.Children.Add(row);
                rows.Add((ax, minBox, maxBox));
            }

            foreach (var ax in xAxes) if (ax is NumericAxisViewModel nx) AddAxisRow(nx, "X Axis");
            foreach (var ax in yAxes) if (ax is NumericAxisViewModel ny) AddAxisRow(ny, "Y Axis");

            if (rows.Count == 0)
            {
                MessageBox.Show("No axes to configure. Run a simulation or add variables first.", "Axis Ranges", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Window
            {
                Title = "Axis Ranges (manual scale)",
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current?.MainWindow,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
            var apply = new Button { Content = "Apply", Width = 80, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            var cancel = new Button { Content = "Cancel", Width = 80, IsCancel = true };
            buttons.Children.Add(apply);
            buttons.Children.Add(cancel);
            panel.Children.Add(buttons);

            apply.Click += (s, e) =>
            {
                // 숫자가 아니거나 Max ≤ Min 인 줄은 알리고 창을 닫지 않는다(이전에는 말없이 건너뛰고 닫혔다, 2026-10-06). 두 칸 다 비우면 그 축은 그대로.
                var bad = new List<string>();
                var good = new List<(NumericAxisViewModel axis, double min, double max)>();
                foreach (var (axis, minBox, maxBox) in rows)
                {
                    if (string.IsNullOrWhiteSpace(minBox.Text) && string.IsNullOrWhiteSpace(maxBox.Text)) continue;
                    if (double.TryParse(minBox.Text, out double min) && double.TryParse(maxBox.Text, out double max) && max > min)
                        good.Add((axis, min, max));
                    else
                        bad.Add(string.IsNullOrWhiteSpace(axis.AxisTitle?.ToString()) ? axis.Id : axis.AxisTitle.ToString());
                }
                if (bad.Count > 0)
                {
                    MessageBox.Show(dialog, "Enter numbers with Max greater than Min for:\n\n" + string.Join("\n", bad) +
                                    "\n\nLeave both boxes empty to keep an axis as it is.", "Axis Ranges", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                foreach (var (axis, min, max) in good)
                {
                    axis.AutoRange = SciChart.Charting.Visuals.Axes.AutoRange.Never;
                    axis.VisibleRange = new DoubleRange(min, max);
                }
                dialog.Close();
            };

            dialog.Content = panel;
            dialog.ShowDialog();
        }
    }
}
