using Basler.Pylon;
using BulbPicker.App.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BulbPicker.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // test
            TestIndexManager.Instance.StartTestStopwatch();
        }

        //private async void Window_Loaded(object sender, RoutedEventArgs e)
        //{
        //    await App.CameraService.FindCamerasAsync();
        //}

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else DragMove();
        }
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}