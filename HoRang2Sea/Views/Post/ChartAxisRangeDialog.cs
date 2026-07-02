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
                foreach (var (axis, minBox, maxBox) in rows)
                {
                    if (double.TryParse(minBox.Text, out double min) &&
                        double.TryParse(maxBox.Text, out double max) && max > min)
                    {
                        axis.AutoRange = SciChart.Charting.Visuals.Axes.AutoRange.Never;
                        axis.VisibleRange = new DoubleRange(min, max);
                    }
                }
                dialog.Close();
            };

            dialog.Content = panel;
            dialog.ShowDialog();
        }
    }
}
