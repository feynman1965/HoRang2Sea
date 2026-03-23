using DevExpress.Mvvm.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoRang2Sea.ViewModels
{
    public abstract class PostWorkspaceViewModel : ViewModel
    {
        protected PostWorkspaceViewModel()
        {
            IsClosed = true;
        }

        public event EventHandler RequestClose;

        public virtual bool IsActive { get; set; }
        [BindableProperty(OnPropertyChangedMethodName = "OnIsClosedChanged")]
        public virtual bool IsClosed { get; set; }
        public virtual bool IsOpened { get; set; }

        public void Close()
        {
            EventHandler handler = RequestClose;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
        protected virtual void OnIsClosedChanged()
        {
            IsOpened = !IsClosed;
        }
    }
}
