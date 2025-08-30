using BulbPicker.App.Infrastructures;
using BulbPicker.App.Services;
using System.Windows.Media.Imaging;

namespace BulbPicker.App.ViewModels
{
    public class CompositeImagesViewModel : ObservableObject
    {
        public BitmapSource FirstRowImage => CompositeImageService.Instance.FirstRowCompositeImageSource;
        public BitmapSource SecondRowImage => null;
    }
}
