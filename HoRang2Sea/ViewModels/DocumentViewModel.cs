using DevExpress.Xpf.Editors;
using HoRang2Sea.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HoRang2Sea.ViewModels
{
    public enum MVVMDirection { FROM, TO };

    public class DocumentViewModel : PanelWorkspaceViewModel
    {
        public DocumentViewModel()
        {
            IsClosed = false;
        }
        public DocumentViewModel(string displayName, string text) : this()
        {
            DisplayName = displayName;
        }
        protected override void OnDispose()
        {
            UpdateToModel();
            if (solutionItem.mymodel != null)
            {
                solutionItem.mymodel.IsClosed = true;
            }
        }

        public virtual void UDPConnect() { }
        public virtual void UDPDisConnect() { }
        public SolutionItem solutionItem { get; set; }
        public string FilePath { get; protected set; } = "";
        protected override string WorkspaceName { get { return "DocumentHost"; } }
        public bool OpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Visual C# Files (*.cs)|*.cs|XAML Files (*.xaml)|*.xaml";
            openFileDialog.FilterIndex = 1;
            bool? dialogResult = openFileDialog.ShowDialog();
            bool dialogResultOK = dialogResult.HasValue && dialogResult.Value;
            if (dialogResultOK)
            {
                DisplayName = openFileDialog.SafeFileName;
                FilePath = openFileDialog.FileName;
                Stream fileStream = File.OpenRead(openFileDialog.FileName);
                using (StreamReader reader = new StreamReader(fileStream))
                {
                }
                fileStream.Close();
            }
            return dialogResultOK;
        }
        public override void OpenItemByPath(string path)
        {
            DisplayName = Path.GetFileName(path);
            FilePath = path;
            IsActive = true;
        }
        public override void OpenItemByItem(SolutionItem item)
        {
            solutionItem = item;
            DisplayName = item.Name;
            IsActive = true;
            if (item.mymodel != null)
                this.UpdateFromModel<Mymodel>(item.mymodel);
        }
        public virtual void UpdateFromModel<TModel>(TModel model)
        {
            this.Update<TModel>(model, MVVMDirection.FROM);
        }

        public virtual void UpdateToModel<TModel>(TModel model)
        {
            this.Update<TModel>(model, MVVMDirection.TO);
        }
        public void UpdateToModel()
        {
            if (solutionItem.mymodel != null)
            {
                UpdateToModel<Mymodel>(solutionItem.mymodel);
            }
        }
        private void Update<TModel>(TModel model, MVVMDirection direction)
        {
            PropertyInfo[] mProperties = model.GetType().GetProperties();
            PropertyInfo[] vmProperties = this.GetType().GetProperties();

            foreach (PropertyInfo mProperty in mProperties)
            {
                PropertyInfo vmProperty = this.GetType().GetProperty(mProperty.Name);
                if (vmProperty != null)
                {
                    if (vmProperty.PropertyType.Equals(mProperty.PropertyType))
                    {
                        if (direction == MVVMDirection.FROM)
                        {
                            vmProperty.SetValue(this, mProperty.GetValue(model));
                        }
                        else
                        {
                            mProperty.SetValue(model, vmProperty.GetValue(this));
                        }
                    }
                    else if (vmProperty.PropertyType.IsGenericType
                        && mProperty.PropertyType.IsGenericType)
                    {
                        Type[] vmDerived = vmProperty.PropertyType.GetGenericArguments();
                        Type[] mDerived = mProperty.PropertyType.GetGenericArguments();
                        Type vmGeneric = vmProperty.PropertyType.GetGenericTypeDefinition();
                        Type mGeneric = mProperty.PropertyType.GetGenericTypeDefinition();
                        if (vmDerived[0].Equals(mDerived[0])
                            && mGeneric.Equals(typeof(List<>))
                            && vmGeneric.Equals(typeof(ObservableCollection<>)))
                        {
                            if (direction == MVVMDirection.FROM)
                            {
                                ConstructorInfo c = vmProperty.PropertyType.GetConstructor(new Type[] { mProperty.PropertyType });
                                object o = c.Invoke(new object[] { mProperty.GetValue(model) });
                                vmProperty.SetValue(this, o);
                            }
                            else
                            {   // To model
                                MethodInfo m = typeof(Enumerable).GetMethod("ToList");
                                var constructedToList = m.MakeGenericMethod(vmDerived);
                                object o = constructedToList.Invoke(vmProperty, new[] { vmProperty.GetValue(this) });
                                mProperty.SetValue(model, o);
                            }
                        }
                    }
                }
            }
        }
    }
}
