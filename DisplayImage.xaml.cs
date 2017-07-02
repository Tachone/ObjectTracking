using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ImageProcess
{
    /// <summary>
    /// DisplayImage.xaml 的互動邏輯
    /// </summary>
    public partial class DisplayImage : Window
    {

        public DisplayImage()
        {
            InitializeComponent();
        }

        public void updateImage(object sender, ImageSource e)
        {
            displayImage.Source = e;
            this.Width = e.Width;
            this.Height = e.Height;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            displayImage.Width = this.Width;
            displayImage.Height = this.Height;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }
    }
}
