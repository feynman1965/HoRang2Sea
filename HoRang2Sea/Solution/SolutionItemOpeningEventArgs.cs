using System;

namespace HoRang2Sea.ViewModels
{
    #region Tool Panels

    public class SolutionItemOpeningEventArgs : EventArgs
    {
        public SolutionItemOpeningEventArgs(SolutionItem solutionItem)
        {
            SolutionItem = solutionItem;
        }

        public SolutionItem SolutionItem { get; set; }
    }
    #endregion
}
