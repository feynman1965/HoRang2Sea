using DevExpress.Mvvm.UI;
using DevExpress.Xpf.Ribbon;

namespace HoRang2Sea.Services
{
    public interface IBackstageViewService
    {
        void Close();
    }
    public class BackstageViewService : ServiceBase, IBackstageViewService
    {
        RibbonControl Ribbon { get { return AssociatedObject as RibbonControl; } }
        public void Close()
        {
            if (Ribbon != null || Ribbon.MergedParent != null)
            {
                if (Ribbon.MergedParent != null)
                    Ribbon.MergedParent.CloseApplicationMenu();
                else
                    Ribbon.CloseApplicationMenu();
            }
        }
    }
}
