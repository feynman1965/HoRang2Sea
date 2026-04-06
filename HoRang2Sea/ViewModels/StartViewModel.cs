namespace HoRang2Sea.ViewModels
{
    public class StartViewModel : PanelWorkspaceViewModel
    {
        protected override string WorkspaceName { get { return "DocumentHost"; } }
        public StartViewModel()
        {
            DisplayName = "Start Page";
            Glyph = Common.CommonFunction.GetGlyphFromUri("pack://application:,,,/DevExpress.Images.v25.1;component/SvgImages/Icon Builder/Actions_Home.svg");
        }
    }
}
