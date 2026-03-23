using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.TreeList;
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
    /// SolutionExplorerView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SolutionExplorerView : UserControl
    {
        public SolutionExplorerView()
        {
            InitializeComponent();
        }

        private void TreeViewItem_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            e.Handled = true;
        }

        private void treeview_NodeDoubleClick(object sender, NodeDoubleClickEventArgs e)
        {
            if (e.HitInfo.RowHandle >= 0)
            {
                SolutionExplorerViewModel vm = DataContext as SolutionExplorerViewModel;
                vm.OpenItem(vm.SelectedItem);
            }
        }

        private void treeview_ShowingEditor(object sender, TreeViewShowingEditorEventArgs e)
        {
            if (sender.ToString() == "")
                e.Cancel = true;
        }
    }
    public class ReportLibraryNodeImageSelector : TreeListNodeImageSelector
    {
        public override ImageSource Select(TreeListRowData rowData)
        {
            var og = ((SolutionItem)rowData.Row).GlyphPath;
            return WpfSvgRenderer.CreateImageSource(new Uri(og));
        }
    }
}
