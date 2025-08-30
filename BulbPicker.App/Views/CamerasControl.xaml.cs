using BulbPicker.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace BulbPicker.App.Views
{
    public partial class CamerasControl : UserControl
    {
        public CamerasControl()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CamerasViewModel vm)
            {
                vm.OnLoadedAsync();
            }
        }
    }
}
