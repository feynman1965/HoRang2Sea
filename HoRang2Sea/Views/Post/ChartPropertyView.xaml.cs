using DevExpress.Xpf.Editors;
using HoRang2Sea.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HoRang2Sea.Views
{
    /// <summary>
    /// ChartPropertyView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChartPropertyView : UserControl
    {
        public ChartPropertyView()
        {
            InitializeComponent();
        }

        private void OnDragRecordOver(object sender, DevExpress.Xpf.Core.DragRecordOverEventArgs e)
        {
             // ChartXItems == 1 return
            if (e.IsFromOutside && typeof(String).IsAssignableFrom(e.GetRecordType()))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }
        private void OnXDragRecordOver(object sender, DevExpress.Xpf.Core.DragRecordOverEventArgs e)
        {
            var ListBoxedit = sender as ListBoxEdit;
            var data = ListBoxedit.ItemsSource as ObservableCollection<string>;

            if (data.Count == 1)
            {
                //MessageBox.Show("Only one data can be selected for the X-axis.");
                return;
            }

            if (e.IsFromOutside && typeof(String).IsAssignableFrom(e.GetRecordType()))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        private void OnYDragRecordOver(object sender, DevExpress.Xpf.Core.DragRecordOverEventArgs e)
        {
            var ListBoxedit = sender as ListBoxEdit;
            var data = ListBoxedit.ItemsSource as ObservableCollection<string>;

            if (data.Count == 4)
            {
                return;
            }

            if (e.IsFromOutside && typeof(String).IsAssignableFrom(e.GetRecordType()))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        // 트리 변수(잎) 더블클릭:
        //  - y-x 모드에서 'Set X-axis' 토글이 켜져 있으면 → X축으로 지정
        //  - 그 외 → Y축 토글(추가됨이면 제거, 아니면 추가). 트리에선 안 빠지고 ✓ 표시만 바뀜.
        private void GlobalTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeView tv && tv.SelectedItem is ChartVariableItem item
                && DataContext is PostChartViewModel vm)
            {
                RunPreservingTreeScroll(tv, () =>
                {
                    if (vm.UseXAxisVariable && XModeToggle.IsChecked == true)
                    {
                        vm.SetXItem(item.Name);
                    }
                    else
                    {
                        if (item.IsAdded) vm.RemoveYItem(item.Name);
                        else vm.AddYItem(item.Name);
                    }
                });
            }
        }

        // 0624 피드백: 선택/해제 시 트리 컬렉션이 통째로 재생성되며 스크롤이 맨 위로 튐 → 오프셋 저장 후 복원.
        private void RunPreservingTreeScroll(TreeView tv, Action action)
        {
            var sv = FindVisualChild<ScrollViewer>(tv);
            double offset = sv?.VerticalOffset ?? 0;
            action();
            if (sv != null)
                Dispatcher.BeginInvoke(new Action(() => sv.ScrollToVerticalOffset(offset)),
                    System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        // 우측 X 목록에서 더블클릭 → 제거
        private void XItemsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxEdit lb && lb.SelectedItem is string name
                && DataContext is PostChartViewModel vm)
            {
                vm.RemoveXItem(name);
            }
        }

        // 우측 Y 목록에서 더블클릭 → 제거
        private void YItemsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxEdit lb && lb.SelectedItem is string name
                && DataContext is PostChartViewModel vm)
            {
                vm.RemoveYItem(name);
            }
        }
    }
}
