using DevExpress.Xpf.Editors;
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
    /// TimeChartPropertyView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TimeChartPropertyView : UserControl
    {
        public TimeChartPropertyView()
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

        // 컴포넌트 트리에서 변수(잎)를 더블클릭 → 토글(추가됨이면 제거, 아니면 추가). 트리에선 안 빠지고 ✓ 표시만 바뀜.
        private void GlobalTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeView tv && tv.SelectedItem is ViewModels.ChartVariableItem item
                && DataContext is ViewModels.PostTimeChartViewModel vm)
            {
                if (item.IsAdded) vm.RemoveYItem(item.Name);
                else vm.AddYItem(item.Name);
            }
        }

        // 우측 Y 목록에서 항목을 더블클릭 → 제거
        private void YItemsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxEdit lb && lb.SelectedItem is string name
                && DataContext is ViewModels.PostTimeChartViewModel vm)
            {
                vm.RemoveYItem(name);
            }
        }

    }
}
