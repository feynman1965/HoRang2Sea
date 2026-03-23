using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HoRang2Sea.Views
{
    /// <summary>
    /// PostTimeChartView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PostTimeChartView : UserControl
    {
        public PostTimeChartView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //UserToolbar.TargetSurface = "{Binding Source={x:Reference Name=sciChart1}}"
        }
    }
}
