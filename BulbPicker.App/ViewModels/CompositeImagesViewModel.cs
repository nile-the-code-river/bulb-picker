using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace BulbPicker.App.ViewModels
{
    internal class CompositeImagesViewModel
    {
        private BitmapSource _firstRowImage;
        public BitmapSource FirstRowImage
        {
            get => _firstRowImage;
            //set { _firstImage = value; OnPropertyChanged(nameof(FirstImage)); }
        }

        private BitmapSource _secondRowImage;
        public BitmapSource SecondRowImage
        {
            get => _secondRowImage;
            //set { _secondImage = value; OnPropertyChanged(nameof(SecondImage)); }
        }

    }
}
