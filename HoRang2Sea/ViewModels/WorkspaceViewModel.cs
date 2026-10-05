using DevExpress.Mvvm.DataAnnotations;
using System;

namespace HoRang2Sea.ViewModels
{
    public abstract class WorkspaceViewModel : ViewModel
    {
        protected WorkspaceViewModel()
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
            if (!ConfirmClose()) return;   // 실행 중인 계산이 있으면 묻는다(문서 탭, 2026-10-05)
            EventHandler handler = RequestClose;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
        /// <summary>닫기 전에 확인(false 면 닫지 않음). 문서 탭이 실행 중인 계산을 묻는 데 쓴다.</summary>
        protected virtual bool ConfirmClose() => true;

        protected virtual void OnIsClosedChanged()
        {
            IsOpened = !IsClosed;
        }
    }
}
