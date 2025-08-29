using BulbPicker.App.Infrastructures;
using System.Windows.Media.Imaging;

// 0830 TODO : Implement
namespace BulbPicker.App.ViewModels
{
    public class CompositeImagesViewModel : ObservableObject
    {
        private BitmapSource _firstRowImage;
        public BitmapSource FirstRowImage
        {
            get => _firstRowImage;
            private set
            {
                _firstRowImage = value;
                OnPropertyChanged(nameof(FirstRowImage));
            }
        }

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
