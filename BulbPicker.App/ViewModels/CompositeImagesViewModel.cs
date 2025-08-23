using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using BulbPicker.App.Services;
using System.Windows.Media.Imaging;

namespace BulbPicker.App.ViewModels
{
    public class CompositeImagesViewModel : ViewModelBase
    {
        private BitmapSource _firstRowImage;
        public BitmapSource FirstRowImage => CompositeImageService.Instance.TestImages.FirstOrDefault();

        private BitmapSource _secondRowImage;
        public BitmapSource SecondRowImage
        {
            get => _secondRowImage;
            private set
            {
                _secondRowImage = value;
                OnPropertyChanged(nameof(SecondRowImage));
            }
        }
    }
}
