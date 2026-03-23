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


            SciChartSurface.SetRuntimeLicenseKey("AcrmWhcNT5lkRmD8o7x6Jw6hJZmQeRZZotGeSqetKfJ4rgmsMa+hTlfyNvSdSUb4tTdY0Ciq0QzfQkGNh5T4RqdJLddbI8m2fTL+CAJheiTf4Ne0s0YrGwQVocGUtRU1h2pXA4IUygx3fH4ZLPuwHrsHPtImEmt7KIg0dgeE+2lUi7OxCZLkAO/oRWM0m2Vxkd76SLWft/t7EW0diJzq7E0EZLW7Cqfufsy1/okz+mbKHy0DkD5AALbL2xkj0gxfxjRd1BLTCzTkvDvApsLZiH1ZJXkQpJI45S5M5sDnnjK6XMHfewLXUXeqe5bmmbF+VO+ZPmfuNNqJY9NibjPJTzDHl1kBxXeA69Pip7bm3K+X+4EutixKWCM+feZy+xIkjP2S0cyWIa32nEn21OTTN+hbNiUeQmT3sHxK6gtZGOk9oSsvNYZtT+jp3hWtn4/WtHp29FibfY3t6Lf89n8JQaJIw6gjm6Dzf17rXMY3lEOfFXIQACI50lqkgeQhV+M22bwh8j5MoZ6e9l/qQqTJLh8/0xyuIJSt8TdOtUCeA3sQyfaQ/ez3qkPTOGVvHmgUsePvSX15QqdBbJX5RBj1nMhcmiiyBwzP7PffbQuqXlIv9oXHQF4t/6kfkEUkT5PY8IJiYF812d3YgCtTgBw0UMgBeoKPJRyybPQNB7X/SUWuRkZ+8Q==");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
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
    }
}
