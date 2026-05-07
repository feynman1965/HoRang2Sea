using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Core;
using HoRang2Sea.Common;
using HoRang2Sea.ViewModels;
using HoRang2Sea.Views;
using SciChart.Charting.Visuals;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Container = SimpleInjector.Container;

namespace HoRang2Sea
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Container Container { get; set; }

        public App()
        {
            SetupDllDirectories();
            SetSciChartLicense();
        }

        private void SetupDllDirectories()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string modelDllDir = System.IO.Path.Combine(baseDir, "ModelDLLs");
                if (!System.IO.Directory.Exists(modelDllDir))
                    System.IO.Directory.CreateDirectory(modelDllDir);
                string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                if (!currentPath.Contains(modelDllDir))
                    Environment.SetEnvironmentVariable("PATH", $"{baseDir};{modelDllDir};{currentPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DLL 경로 설정 실패: {ex.Message}");
            }
        }

        private void SetSciChartLicense()
        {
            try
            {
                SciChartSurface.SetRuntimeLicenseKey("AcrmWhcNT5lkRmD8o7x6Jw6hJZmQeRZZotGeSqetKfJ4rgmsMa+hTlfyNvSdSUb4tTdY0Ciq0QzfQkGNh5T4RqdJLddbI8m2fTL+CAJheiTf4Ne0s0YrGwQVocGUtRU1h2pXA4IUygx3fH4ZLPuwHrsHPtImEmt7KIg0dgeE+2lUi7OxCZLkAO/oRWM0m2Vxkd76SLWft/t7EW0diJzq7E0EZLW7Cqfufsy1/okz+mbKHy0DkD5AALbL2xkj0gxfxjRd1BLTCzTkvDvApsLZiH1ZJXkQpJI45S5M5sDnnjK6XMHfewLXUXeqe5bmmbF+VO+ZPmfuNNqJY9NibjPJTzDHl1kBxXeA69Pip7bm3K+X+4EutixKWCM+feZy+xIkjP2S0cyWIa32nEn21OTTN+hbNiUeQmT3sHxK6gtZGOk9oSsvNYZtT+jp3hWtn4/WtHp29FibfY3t6Lf89n8JQaJIw6gjm6Dzf17rXMY3lEOfFXIQACI50lqkgeQhV+M22bwh8j5MoZ6e9l/qQqTJLh8/0xyuIJSt8TdOtUCeA3sQyfaQ/ez3qkPTOGVvHmgUsePvSX15QqdBbJX5RBj1nMhcmiiyBwzP7PffbQuqXlIv9oXHQF4t/6kfkEUkT5PY8IJiYF812d3YgCtTgBw0UMgBeoKPJRyybPQNB7X/SUWuRkZ+8Q==");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SciChart 라이센스 설정 실패: {ex.Message}");
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                LogException("AppDomain.UnhandledException", args.ExceptionObject as Exception);
            };
            DispatcherUnhandledException += (s, args) =>
            {
                LogException("DispatcherUnhandledException", args.Exception);
                args.Handled = true;
            };
            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                LogException("TaskScheduler.UnobservedTaskException", args.Exception);
                args.SetObserved();
            };

            SetSciChartLicense();

            base.OnStartup(e);
            Container = new Container();
            Container.Options.EnableAutoVerification = false;
            Container.RegisterSingleton(typeof(OutputViewModel), ViewModelSource.GetPOCOType(typeof(OutputViewModel)));
            Container.RegisterSingleton(typeof(MainViewModel), ViewModelSource.GetPOCOType(typeof(MainViewModel)));
            Container.RegisterSingleton(typeof(MonitorViewModel), ViewModelSource.GetPOCOType(typeof(MonitorViewModel)));
            Container.RegisterSingleton(typeof(Solution), ViewModelSource.GetPOCOType(typeof(Solution)));
            //Container.RegisterSingleton(typeof(PropertyViewModel), ViewModelSource.GetPOCOType(typeof(PropertyViewModel)));

            DISource.Resolver = Resolve;
            //Container.Verify();
        }
        object Resolve(Type type, object key, string name) => type == null ? null : Container.GetInstance(type);

        private void LogException(string source, Exception ex)
        {
            try
            {
                string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
                string message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}\n{ex?.ToString()}\n\n";
                System.IO.File.AppendAllText(logPath, message);
                MessageBox.Show($"오류가 발생했습니다.\n{ex?.Message}\n\n자세한 내용은 error_log.txt를 확인하세요.",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch { }
        }
    }
}
