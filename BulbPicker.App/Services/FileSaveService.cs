using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace BulbPicker.App.Services
{
    public enum FolderName
    {
        ImageComposition,
        SingleImageGrabbed,
        BoundingBoxImage
    }

    public static class FileSaveService
    {
        public static void SaveBitmapTo(Bitmap bitmap, FolderName folderName, string fileName)
        {
            string dirPath = GetTestFolderPath(folderName);
            Directory.CreateDirectory(dirPath);

            string filePath = Path.Combine(dirPath, $"{fileName}.bmp");
            bitmap.Save(filePath, ImageFormat.Bmp);
        }

        public static string GetTestFolderPath(FolderName folderName)
        {
            string folderNameStr = "default-folder-name";

            switch (folderName)
            {
                case FolderName.ImageComposition:
                    folderNameStr = "image-composition";
                    break;
                case FolderName.SingleImageGrabbed:
                    folderNameStr = "image-grabbed";
                    break;
                case FolderName.BoundingBoxImage:
                    folderNameStr = "bounding-box";
                    break;
                default:
                    MessageBox.Show("ERROR: Invalid Folder Name Chosen");
                    break;
            }

            return Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "_test-result",
                    folderNameStr,
                    TestIndexManager.Instance.ManagedDateTimeStr);
        }
    }
}
