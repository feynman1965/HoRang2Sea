using DevExpress.Xpf.Docking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoRang2Sea.ViewModels
{
    abstract public class PostPanelWorkspaceViewModel : PostWorkspaceViewModel, IMVVMDockingProperties
    {
        string _targetName;

        protected PostPanelWorkspaceViewModel()
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
