using DevExpress.Mvvm.POCO;
using System;
using System.Windows.Media;

namespace HoRang2Sea.ViewModels
{
    public class SolutionExplorerViewModel : PanelWorkspaceViewModel
    {
        public SolutionExplorerViewModel()
        {
            Solution = App.Container.GetInstance<Solution>();
            DisplayName = "Solution Explorer";
            Glyph = Common.CommonFunction.GetGlyphFromUri("pack://application:,,,/DevExpress.Images.v22.2;component/SvgImages/Dashboards/AutoExpand.svg");
        }

        public event EventHandler<SolutionItemOpeningEventArgs> ItemOpening;
        public Solution Solution { get; set; }
        public SolutionItem SelectedItem { get; set; }
        protected override string WorkspaceName { get { return "RightHost"; } }

        public void OpenItem(SolutionItem item)
        {
            if (item != null && ItemOpening != null)
                ItemOpening.Invoke(this, new SolutionItemOpeningEventArgs(item));
        }

        public void OnDeleteNode()
        {
            Solution.Items.Remove(SelectedItem);
        }
    }
}
