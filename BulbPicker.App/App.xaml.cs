using BulbPicker.App.Services;
using System.Configuration;
using System.Data;
using System.Windows;

namespace BulbPicker.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static CameraService CameraService { get; } = CameraService.Instance;

    }

}
