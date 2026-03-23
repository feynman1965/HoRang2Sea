using DevExpress.Utils;
using DevExpress.Utils.Svg;
using DevExpress.Xpf.Core;
using HoRang2Sea.Common;
using System;
using System.Windows.Media;

namespace HoRang2Sea.ViewModels
{
    public class OutputViewModel : PanelWorkspaceViewModel
    {
        public OutputViewModel()
        {
            DisplayName = "Output";
            Glyph = CommonFunction.GetGlyphFromUri(" pack://application:,,,/DevExpress.Images.v22.2;component/SvgImages/Icon Builder/Actions_Window.svg");
            Text = @"1>------ Build started: Project: VisualStudioInspiredUIDemo, Configuration: Debug Any CPU ------
1>  DockingDemo -> C:\VisualStudioInspiredUIDemo.exe
========== Build: 1 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========";
        }

        public string Text { get; private set; }
        protected override string WorkspaceName { get { return "BottomHost"; } }

        static OutputViewModel _OutputViewModel = new OutputViewModel();
        static OutputViewModel Instance => _OutputViewModel;
    }
}
