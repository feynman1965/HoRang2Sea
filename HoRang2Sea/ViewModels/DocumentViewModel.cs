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
            StopSimulation();   // 탭을 닫으면 계산 스레드와 DLL 도 정리한다(이전에는 탭을 닫아도 계산이 계속 돌았다, 2026-10-05)
            UpdateToModel();
            if (solutionItem.mymodel != null)
            {
                solutionItem.mymodel.IsClosed = true;
            }
        }

        // ── 실행 상태 (2026-10-05) ──
        private GenericPortDllModel RunModel => solutionItem?.mymodel?.BaseMWModel as GenericPortDllModel;
        /// <summary>이 탭의 계산이 실행 또는 일시정지 중인지.</summary>
        public bool IsSimulationActive => RunModel?.IsSimulationActive == true;
        /// <summary>계산을 멈추고 DLL 을 내린다(그래프 · 멈춘 위치 · 기록은 남김).</summary>
        public void StopSimulation() { try { RunModel?.StopCalculation(keepResults: true); } catch { } }

        /// <summary>실행 · 일시정지 중이면 알리고 true(그 동작을 하지 않음). 실행 중에 바꾼 프로파일 · 설정 · 모드는
        /// 이번 실행에 들어가지 않아 화면과 실제가 달라지므로 막는다(2026-10-05).</summary>
        protected bool BlockWhileRunning(string action)
        {
            if (!IsSimulationActive) return false;
            System.Windows.MessageBox.Show(System.Windows.Application.Current?.MainWindow,
                $"A simulation is running. Stop it before {action}.",
                "Simulation running", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return true;
        }

        protected override bool ConfirmClose()
        {
            if (!IsSimulationActive) return true;
            var r = System.Windows.MessageBox.Show(System.Windows.Application.Current?.MainWindow,
                $"A simulation is running in '{DisplayName}'.\n\nStop it and close the tab? Results that were not exported will be lost.",
                "Close", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (r != System.Windows.MessageBoxResult.Yes) return false;
            StopSimulation();
            return true;
        }

        public virtual void UDPConnect() { }
        public virtual void UDPDisConnect() { }
        // Home "Saved Configs" / 모델별 Save·Load Config 공용 hook (각 모델 VM이 override).
        public virtual void SaveConfig() { }
        public virtual void LoadConfig(string path) { }
        // Run 시 현재 입력 스냅샷을 History(_history, 최근 20개)에 자동 저장 (각 모델 VM이 override).
        public virtual void SaveConfigToHistory() { }
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
            // 다른 차량 Status가 남는 문제 방지: 활성화 시 Status를 차량명+Ready로 리셋.
            try { var mv = App.Container.GetInstance<MainViewModel>(); if (mv != null) mv.Status = $"{item.Name} Ready"; } catch { }
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
