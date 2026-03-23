using DevExpress.Mvvm;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Utils;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace HoRang2Sea.ViewModels
{
    public abstract class ViewModel : ViewModelBase, IDisposable
    {
        public string BindableName { get { return GetBindableName(DisplayName); } }
        public virtual string DisplayName { get; protected set; }
        public virtual ImageSource Glyph { get; set; }

        string GetBindableName(string name) { return "_" + Regex.Replace(name, @"\W", ""); }

        #region IDisposable Members
        public void Dispose()
        {
            OnDispose();
        }
        protected virtual void OnDispose() { }
#if DEBUG
        ~ViewModel()
        {
            string msg = string.Format("{0} ({1}) ({2}) Finalized", GetType().Name, DisplayName, GetHashCode());
            System.Diagnostics.Debug.WriteLine(msg);
        }
#endif
        #endregion 
    }
    #region Bars
    public class BarModel : ViewModel
    {
        public BarModel(string displayName)
        {
            DisplayName = displayName;
        }
        public List<CommandViewModel> Commands { get; set; }
        public bool IsMainMenu { get; set; }
    }

    public class CommandViewModel : ViewModel
    {

        public CommandViewModel() { }
        public CommandViewModel(string displayName, List<CommandViewModel> subCommands)
            : this(displayName, null, null, subCommands)
        {
        }
        public CommandViewModel(string displayName, ICommand command = null)
            : this(displayName, null, command, null)
        {
        }
        public CommandViewModel(WorkspaceViewModel owner, ICommand command)
            : this(string.Empty, owner, command)
        {
        }
        private CommandViewModel(string displayName, WorkspaceViewModel owner = null, ICommand command = null, List<CommandViewModel> subCommands = null)
        {
            IsEnabled = true;
            Owner = owner;
            if (Owner != null)
            {
                DisplayName = Owner.DisplayName;
                Glyph = Owner.Glyph;
            }
            else DisplayName = displayName;
            Command = command;
            Commands = subCommands;
        }

        public ICommand Command { get; private set; }
        public List<CommandViewModel> Commands { get; set; }
        public BarItemDisplayMode DisplayMode { get; set; }
        public bool IsComboBox { get; set; }
        public bool IsEnabled { get => GetValue<bool>(); set => SetValue(value); }
        public bool IsSeparator { get; set; }
        public bool IsSubItem { get; set; }
        public KeyGesture KeyGesture { get; set; }
        public WorkspaceViewModel Owner { get; private set; }
    }
    #endregion
}
