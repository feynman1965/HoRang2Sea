using DevExpress.Xpf.Docking;
using System.Reflection;

namespace HoRang2Sea.ViewModels
{
    abstract public class PanelWorkspaceViewModel : WorkspaceViewModel, IMVVMDockingProperties
    {
        string _targetName;

        protected PanelWorkspaceViewModel()
        {
            _targetName = WorkspaceName;
        }

        abstract protected string WorkspaceName { get; }
        string IMVVMDockingProperties.TargetName
        {
            get { return _targetName; }
            set { _targetName = value; }
        }

        public virtual void OpenItemByPath(string path) { }

        public virtual void OpenItemByItem(SolutionItem item) { }

    }
}
