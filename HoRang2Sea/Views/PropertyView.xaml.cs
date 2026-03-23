using DevExpress.Xpf.Grid;
using DevExpress.XtraGrid;
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
    /// PropertyView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PropertyView : UserControl
    {
        public PropertyView()
        {
            InitializeComponent();
            gridControl.GroupBy("Name");
        }

        private Point startPoint;
        private bool isDragging;

        private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 휠의 회전 방향을 가져옵니다.
            var delta = e.Delta > 0 ? 0.1 : -0.1;

            // ScaleTransform의 ScaleX 및 ScaleY 속성을 조정하여 이미지를 확대/축소합니다.
            scaleTransform.ScaleX += delta;
            scaleTransform.ScaleY += delta;

            // 이벤트 처리를 중지합니다.
            e.Handled = true;
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                startPoint = e.GetPosition(null);
                isDragging = true;
            }
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                var currentPosition = e.GetPosition(null);
                var delta = currentPosition - startPoint;

                // 이미지의 중심 좌표를 이동합니다.
                translateTransform.X += delta.X;
                translateTransform.Y += delta.Y;

                startPoint = currentPosition;
            }
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released)
            {
                isDragging = false;
            }
        }
    }
}
