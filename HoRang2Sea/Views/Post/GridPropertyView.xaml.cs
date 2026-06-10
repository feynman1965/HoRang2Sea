using DevExpress.Xpf.Editors;
using HoRang2Sea.ViewModels;
using System;
using System.Collections.Generic;
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
    /// GridPropertyView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GridPropertyView : UserControl
    {
        public GridPropertyView()
        {
            InitializeComponent();
        }

        private void OnDragRecordOver(object sender, DevExpress.Xpf.Core.DragRecordOverEventArgs e)
        {
            if (e.IsFromOutside && typeof(String).IsAssignableFrom(e.GetRecordType()))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        // 트리 변수(잎) 더블클릭 → This Items 토글(추가됨이면 제거, 아니면 추가). 트리에선 안 빠지고 ✓ 표시만 바뀜.
        private void GlobalTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeView tv && tv.SelectedItem is ChartVariableItem item
                && DataContext is PostGridViewModel vm)
            {
                if (item.IsAdded) vm.RemoveGridItem(item.Name);
                else vm.AddGridItem(item.Name);
            }
        }

        // 우측 This Items 목록에서 더블클릭 → 제거
        private void ThisItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxEdit lb && lb.SelectedItem is string name
                && DataContext is PostGridViewModel vm)
            {
                vm.RemoveGridItem(name);
            }
        }
    }
}
