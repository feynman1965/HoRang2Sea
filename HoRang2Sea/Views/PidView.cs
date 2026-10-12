using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HoRang2Sea.Models;
using ColumnDefinition = HoRang2Sea.Models.ColumnDefinition;   // System.Windows.Controls.ColumnDefinition 과 이름이 같음

namespace HoRang2Sea.Views
{
    /// <summary>
    /// 입력 화면 다이어그램 자리에 대학 P&ID 그림을 보여 준다(2026-10-13).
    /// - 그림 · 누를 영역: Resource/Pid/pid_map.json + *.png (배포자료/03_기획문서/PID_앱적용_1013/build_pid_assets.py 가 만듦)
    /// - 찾는 키: 모델(뷰모델 이름에서 "ModuleViewModel" 뺀 것) → 레이아웃 D{DesignLayout}C{ControlLayout}
    /// - 부품을 누르면 예전 다이어그램 클릭과 같이 Property 패널(Data · OutputData · PropertyImagePath)을 채운다.
    /// - 그림에 없는 부품은 위쪽 버튼으로 고른다. Property 의 Show 로 켠 값은 그림의 부품 아래에 표시한다.
    /// 뷰모델은 차종마다 다른 클래스라 공통 속성(Database · Data · OutputData · PropertyImagePath · DesignLayout · ControlLayout)을 이름으로 읽는다.
    /// </summary>
    public class PidView : UserControl
    {
        private sealed class PidSpot
        {
            public string Table;
            public Rect Box;
        }

        private sealed class PidLayout
        {
            public string Image;
            public List<PidSpot> Spots = new List<PidSpot>();
        }

        private static readonly Brush Accent = Frozen(Color.FromRgb(0x2F, 0x55, 0x97));
        private static readonly Brush HoverFill = Frozen(Color.FromArgb(0x26, 0x2F, 0x55, 0x97));
        private static readonly Brush SelectedFill = Frozen(Color.FromArgb(0x3A, 0x2F, 0x55, 0x97));
        private static readonly Brush ChipFill = Frozen(Color.FromRgb(0xEE, 0xF2, 0xF6));
        private static readonly Brush ChipBorder = Frozen(Color.FromRgb(0xC5, 0xCE, 0xD8));
        private static readonly Brush MutedText = Frozen(Color.FromRgb(0x78, 0x86, 0x94));

        private static Dictionary<string, Dictionary<string, PidLayout>> _map;
        private static double _mapW = 2400, _mapH = 1350;

        private readonly WrapPanel _chips = new WrapPanel { Margin = new Thickness(4, 3, 4, 3) };
        private readonly Grid _viewport = new Grid { ClipToBounds = true, Background = Brushes.White };
        private readonly Viewbox _viewbox = new Viewbox { Stretch = Stretch.Uniform };
        private readonly Canvas _canvas = new Canvas();
        private readonly ScaleTransform _scale = new ScaleTransform(1, 1);
        private readonly TranslateTransform _shift = new TranslateTransform(0, 0);
        private readonly TextBlock _missing = new TextBlock
        {
            Text = "P&ID drawing for this layout is not available yet.\nUse the component buttons above to open each component.",
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 14,
            Foreground = Frozen(Color.FromRgb(0x54, 0x6E, 0x7A)),
            Visibility = Visibility.Collapsed,
        };
        private readonly TextBlock _hint = new TextBlock
        {
            Text = "Click a component to see its properties  ·  Wheel: zoom  ·  Drag: pan  ·  Double-click: fit",
            FontSize = 11,
            Foreground = Frozen(Color.FromRgb(0x90, 0xA4, 0xAE)),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(8, 1, 8, 2),
            IsHitTestVisible = false,
        };
        private readonly Border _hintBar = new Border();   // 그림 아래 띠(안내 글이 그림에 겹치지 않게)
        private readonly StackPanel _offList = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(8, 0, 0, 8),
            IsHitTestVisible = false,
        };

        private readonly List<Border> _spots = new List<Border>();
        private readonly List<INotifyCollectionChanged> _watched = new List<INotifyCollectionChanged>();
        private object _vm;
        private PidLayout _layout;
        private string _selected;
        private Point _down;
        private Vector _downShift;
        private bool _pressed, _dragged;

        public PidView()
        {
            _viewbox.Child = _canvas;
            _viewbox.RenderTransform = new TransformGroup { Children = { _scale, _shift } };
            _viewport.Children.Add(_viewbox);
            _viewport.Children.Add(_missing);
            _viewport.Children.Add(_offList);

            var top = new Border
            {
                Background = Frozen(Color.FromRgb(0xF8, 0xF9, 0xFA)),
                BorderBrush = Frozen(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = _chips,
            };
            DockPanel.SetDock(top, Dock.Top);
            _hintBar.Background = Frozen(Color.FromRgb(0xF8, 0xF9, 0xFA));
            _hintBar.BorderBrush = Frozen(Color.FromRgb(0xE0, 0xE0, 0xE0));
            _hintBar.BorderThickness = new Thickness(0, 1, 0, 0);
            _hintBar.Child = _hint;
            DockPanel.SetDock(_hintBar, Dock.Bottom);
            var root = new DockPanel { LastChildFill = true };
            root.Children.Add(top);
            root.Children.Add(_hintBar);
            root.Children.Add(_viewport);
            Content = root;

            _viewport.PreviewMouseWheel += OnWheel;
            _viewport.PreviewMouseLeftButtonDown += OnDown;
            _viewport.PreviewMouseMove += OnMove;
            _viewport.PreviewMouseLeftButtonUp += OnUp;
            DataContextChanged += (s, e) => { Attach(e.NewValue); Refresh(); };
        }

        private static Brush Frozen(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        // ───────── 그림 목록(pid_map.json) ─────────
        private static Dictionary<string, Dictionary<string, PidLayout>> Map
        {
            get
            {
                if (_map != null) return _map;
                _map = new Dictionary<string, Dictionary<string, PidLayout>>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    var asm = Assembly.GetExecutingAssembly().GetName().Name;
                    var info = Application.GetResourceStream(new Uri($"/{asm};component/Resource/Pid/pid_map.json", UriKind.Relative));
                    if (info == null) return _map;
                    using (var doc = JsonDocument.Parse(info.Stream))
                    {
                        var root = doc.RootElement;
                        _mapW = root.GetProperty("w").GetDouble();
                        _mapH = root.GetProperty("h").GetDouble();
                        foreach (var model in root.GetProperty("models").EnumerateObject())
                        {
                            var layouts = new Dictionary<string, PidLayout>(StringComparer.OrdinalIgnoreCase);
                            foreach (var lay in model.Value.EnumerateObject())
                            {
                                var pl = new PidLayout { Image = lay.Value.GetProperty("img").GetString() };
                                foreach (var s in lay.Value.GetProperty("spots").EnumerateArray())
                                    pl.Spots.Add(new PidSpot
                                    {
                                        Table = s[0].GetString(),
                                        Box = new Rect(s[1].GetDouble(), s[2].GetDouble(), s[3].GetDouble(), s[4].GetDouble()),
                                    });
                                layouts[lay.Name] = pl;
                            }
                            _map[model.Name] = layouts;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"PidView: pid_map.json 읽기 실패 — {ex.Message}");
                }
                return _map;
            }
        }

        // ───────── 뷰모델 ─────────
        private void Attach(object vm)
        {
            if (_vm is INotifyPropertyChanged old) old.PropertyChanged -= OnVmChanged;
            _vm = vm;
            _selected = null;
            if (_vm is INotifyPropertyChanged now) now.PropertyChanged += OnVmChanged;
        }

        private void OnVmChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DesignLayout" || e.PropertyName == "ControlLayout" || e.PropertyName == "Database")
                Dispatcher.BeginInvoke(new Action(Refresh));
        }

        private object VmProp(string name)
        {
            try { return _vm?.GetType().GetProperty(name)?.GetValue(_vm); }
            catch { return null; }
        }

        private DatabaseDefinition Database => VmProp("Database") as DatabaseDefinition;

        private string LayoutKey()
        {
            int d = VmProp("DesignLayout") is int a ? a : 0;
            int c = VmProp("ControlLayout") is int b ? b : 0;
            return $"D{d}C{c}";
        }

        private string ModelName()
        {
            // 실행 중 뷰모델은 DevExpress 가 만든 하위 클래스(CarnivalModuleViewModel_<guid>)라 상위 클래스에서 찾는다
            const string suffix = "ModuleViewModel";
            for (var t = _vm?.GetType(); t != null; t = t.BaseType)
                if (t.Name.EndsWith(suffix)) return t.Name.Substring(0, t.Name.Length - suffix.Length);
            return null;
        }

        private TableDefinition FindTable(string name)
        {
            var db = Database;
            return db?.Tables?.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>예전 다이어그램 클릭(DiagramDoubleClicked)과 같은 일: Property 패널에 그 부품의 입력 · 출력 · 상세 그림.</summary>
        private void Select(string tableName)
        {
            var t = FindTable(tableName);
            if (t == null) return;
            if (VmProp("Data") is IList data)
            {
                data.Clear();
                foreach (var c in t.Columns) data.Add(c);
            }
            if (VmProp("OutputData") is IList outs)
            {
                outs.Clear();
                foreach (var o in t.OutputColumns) outs.Add(o);
            }
            var p = _vm.GetType().GetProperty("PropertyImagePath");
            if (p != null && p.CanWrite) p.SetValue(_vm, @"/Resource/Detail/" + t.Name + @"Detail.svg");
            _selected = t.Name;
            UpdateSelection();
        }

        // ───────── 화면 ─────────
        private void Refresh()
        {
            foreach (var w in _watched) w.CollectionChanged -= OnShownChanged;
            _watched.Clear();
            _canvas.Children.Clear();
            _spots.Clear();
            ResetZoom();

            var db = Database;
            _layout = null;
            var model = ModelName();
            if (model != null && Map.TryGetValue(model, out var layouts)) layouts.TryGetValue(LayoutKey(), out _layout);

            var img = _layout == null ? null : LoadImage(_layout.Image);
            if (img == null)
            {
                _layout = null;
                _viewbox.Visibility = Visibility.Collapsed;
                _hintBar.Visibility = Visibility.Collapsed;
                _missing.Visibility = _vm == null ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                _viewbox.Visibility = Visibility.Visible;
                _hintBar.Visibility = Visibility.Visible;
                _missing.Visibility = Visibility.Collapsed;
                _canvas.Width = _mapW;
                _canvas.Height = _mapH;
                _canvas.Children.Add(new Image { Source = img, Width = _mapW, Height = _mapH, IsHitTestVisible = false });
                // 큰 영역을 먼저 깔고 작은 영역을 위에(겹친 곳은 작은 부품이 눌린다)
                foreach (var s in _layout.Spots.OrderByDescending(s => s.Box.Width * s.Box.Height))
                {
                    var b = new Border
                    {
                        Width = s.Box.Width,
                        Height = s.Box.Height,
                        Background = Brushes.Transparent,
                        BorderBrush = Accent,
                        CornerRadius = new CornerRadius(8),
                        Cursor = Cursors.Hand,
                        Tag = s.Table,
                        ToolTip = s.Table,
                    };
                    Canvas.SetLeft(b, s.Box.X);
                    Canvas.SetTop(b, s.Box.Y);
                    b.MouseEnter += (o, e) => StyleSpot((Border)o, true);
                    b.MouseLeave += (o, e) => StyleSpot((Border)o, false);
                    b.MouseLeftButtonUp += (o, e) => { if (!_dragged) Select((string)((Border)o).Tag); };
                    _canvas.Children.Add(b);
                    _spots.Add(b);
                }
            }

            BuildChips(db);
            if (db?.Tables != null)
                foreach (var t in db.Tables)
                    if (t.VisibleData is INotifyCollectionChanged n)
                    {
                        n.CollectionChanged += OnShownChanged;
                        _watched.Add(n);
                    }
            BuildShownValues();
            UpdateSelection();
        }

        private static BitmapImage LoadImage(string file)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly().GetName().Name;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri($"pack://application:,,,/{asm};component/Resource/Pid/{file}", UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PidView: 그림 {file} 읽기 실패 — {ex.Message}");
                return null;
            }
        }

        private void BuildChips(DatabaseDefinition db)
        {
            _chips.Children.Clear();
            if (db?.Tables == null) return;
            var inDrawing = new HashSet<string>(_layout?.Spots.Select(s => s.Table) ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            _chips.Children.Add(new TextBlock
            {
                Text = "Components",
                FontWeight = FontWeights.SemiBold,
                Foreground = MutedText,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 6, 0),
            });
            foreach (var t in db.Tables)
            {
                var onDrawing = inDrawing.Contains(t.Name);
                var chip = new Border
                {
                    Tag = t.Name,
                    CornerRadius = new CornerRadius(3),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(7, 0, 7, 1),
                    Margin = new Thickness(2, 1, 2, 1),
                    Cursor = Cursors.Hand,
                    ToolTip = onDrawing ? t.Name : t.Name + " (not in this drawing)",
                    Child = new TextBlock { Text = t.Name, FontSize = 11, FontStyle = onDrawing ? FontStyles.Normal : FontStyles.Italic },
                };
                chip.MouseLeftButtonUp += (o, e) => Select((string)((Border)o).Tag);
                _chips.Children.Add(chip);
            }
        }

        private void StyleSpot(Border b, bool hover)
        {
            bool sel = string.Equals((string)b.Tag, _selected, StringComparison.OrdinalIgnoreCase);
            b.Background = sel ? SelectedFill : hover ? HoverFill : Brushes.Transparent;
            b.BorderThickness = new Thickness(sel ? 8 : hover ? 5 : 0);
        }

        private void UpdateSelection()
        {
            foreach (var b in _spots) StyleSpot(b, b.IsMouseOver);
            var inDrawing = new HashSet<string>(_layout?.Spots.Select(s => s.Table) ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var chip in _chips.Children.OfType<Border>())
            {
                var name = (string)chip.Tag;
                bool sel = string.Equals(name, _selected, StringComparison.OrdinalIgnoreCase);
                chip.Background = sel ? Accent : ChipFill;
                chip.BorderBrush = sel ? Accent : ChipBorder;
                ((TextBlock)chip.Child).Foreground = sel ? Brushes.White : inDrawing.Contains(name) ? Brushes.Black : MutedText;
            }
        }

        // ───────── Property 의 Show 로 켠 값 ─────────
        private void OnShownChanged(object sender, NotifyCollectionChangedEventArgs e) => BuildShownValues();

        private void BuildShownValues()
        {
            foreach (var old in _canvas.Children.OfType<FrameworkElement>().Where(c => "values".Equals(c.Tag)).ToList())
                _canvas.Children.Remove(old);
            _offList.Children.Clear();
            var db = Database;
            if (db?.Tables == null) return;
            foreach (var t in db.Tables)
            {
                var shown = (t.VisibleData ?? new System.Collections.ObjectModel.ObservableCollection<object>())
                    .Where(x => x is ColumnDefinition || x is OutputDefinition).ToList();
                if (shown.Count == 0) continue;
                var spot = _layout?.Spots.FirstOrDefault(s => string.Equals(s.Table, t.Name, StringComparison.OrdinalIgnoreCase));
                if (spot != null)
                {
                    var panel = new StackPanel();
                    foreach (var x in shown) panel.Children.Add(ValueLine(x, null, 24));
                    var box = new Border
                    {
                        Tag = "values",
                        Child = panel,
                        Background = Frozen(Color.FromArgb(0xEE, 0xFF, 0xFF, 0xFF)),
                        BorderBrush = Accent,
                        BorderThickness = new Thickness(2),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(6, 2, 6, 3),
                        IsHitTestVisible = false,
                    };
                    // 부품 아래에 두되, 그림 밖으로 나가면 부품 위로
                    box.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    var size = box.DesiredSize;
                    double y = spot.Box.Bottom + 4;
                    if (y + size.Height > _mapH) y = Math.Max(0, spot.Box.Y - size.Height - 4);
                    Canvas.SetLeft(box, Math.Max(0, Math.Min(spot.Box.X, _mapW - size.Width)));
                    Canvas.SetTop(box, y);
                    Panel.SetZIndex(box, 10);
                    _canvas.Children.Add(box);
                }
                else
                {
                    // 그림에 없는 부품(또는 그림이 없는 레이아웃) — 왼쪽 아래 목록
                    foreach (var x in shown) _offList.Children.Add(ValueLine(x, t.Name, 11));
                }
            }
            _offList.Background = _offList.Children.Count > 0 ? Frozen(Color.FromArgb(0xEE, 0xFF, 0xFF, 0xFF)) : null;
        }

        private static TextBlock ValueLine(object item, string table, double size)
        {
            var tb = new TextBlock { FontSize = size };
            if (table != null) tb.Inlines.Add(new Run(table + "  ") { Foreground = MutedText });
            if (item is ColumnDefinition c)
            {
                tb.Inlines.Add(new Run(c.Name + " = "));
                var v = new Run { FontWeight = FontWeights.SemiBold, Foreground = Accent };
                v.SetBinding(Run.TextProperty, new Binding(nameof(ColumnDefinition.Init)) { Source = c, Mode = BindingMode.OneWay });
                tb.Inlines.Add(v);
                if (!string.IsNullOrEmpty(c.Unit)) tb.Inlines.Add(new Run(" " + c.Unit) { Foreground = MutedText });
            }
            else if (item is OutputDefinition o)
            {
                tb.Inlines.Add(new Run(o.Name + " = "));
                var v = new Run { FontWeight = FontWeights.SemiBold, Foreground = Brushes.DarkGreen };
                v.SetBinding(Run.TextProperty, new Binding(nameof(OutputDefinition.Value)) { Source = o, Mode = BindingMode.OneWay });
                tb.Inlines.Add(v);
                if (!string.IsNullOrEmpty(o.Unit)) tb.Inlines.Add(new Run(" " + o.Unit) { Foreground = MutedText });
            }
            return tb;
        }

        // ───────── 확대 · 이동 ─────────
        private void ResetZoom()
        {
            _scale.ScaleX = _scale.ScaleY = 1;
            _shift.X = _shift.Y = 0;
        }

        private void OnWheel(object sender, MouseWheelEventArgs e)
        {
            if (_layout == null) return;
            var m = e.GetPosition(_viewport);
            double s = _scale.ScaleX;
            double ns = Math.Max(1, Math.Min(8, s * (e.Delta > 0 ? 1.2 : 1 / 1.2)));
            double f = ns / s;
            _shift.X = m.X - f * (m.X - _shift.X);
            _shift.Y = m.Y - f * (m.Y - _shift.Y);
            _scale.ScaleX = _scale.ScaleY = ns;
            if (ns <= 1.0001) ResetZoom();
            e.Handled = true;
        }

        private void OnDown(object sender, MouseButtonEventArgs e)
        {
            _dragged = false;
            if (e.ClickCount == 2 && !(e.OriginalSource is Border b && b.Tag is string))
            {
                ResetZoom();
                e.Handled = true;
                return;
            }
            _pressed = _scale.ScaleX > 1.0001;
            _down = e.GetPosition(_viewport);
            _downShift = new Vector(_shift.X, _shift.Y);
        }

        private void OnMove(object sender, MouseEventArgs e)
        {
            if (!_pressed || e.LeftButton != MouseButtonState.Pressed) return;
            var p = e.GetPosition(_viewport);
            if (!_dragged && (p - _down).Length < 5) return;
            if (!_dragged)
            {
                _dragged = true;
                _viewport.CaptureMouse();
            }
            _shift.X = _downShift.X + (p.X - _down.X);
            _shift.Y = _downShift.Y + (p.Y - _down.Y);
        }

        private void OnUp(object sender, MouseButtonEventArgs e)
        {
            _pressed = false;
            if (_dragged)
            {
                _viewport.ReleaseMouseCapture();
                e.Handled = true;
            }
        }
    }
}
