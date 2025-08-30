using BulbPicker.App.Models;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows;

namespace BulbPicker.App.Services
{
    public enum FolderName
    {
        ImageComposition,
        SingleImageGrabbed,
        BoundingBoxImage,
        Log,
        ERROR
    }

    public static class FileSaveService
    {
        async public static Task SaveLogsAsync(ObservableCollection<Log> logs)
        {
            string dirPath = GetResultFolderPath(FolderName.Log);
            Directory.CreateDirectory(dirPath);

            string filePath = Path.Combine(dirPath, "log.txt");

            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            foreach (var log in logs)
                await writer.WriteLineAsync($"[{log.LoggedAt:yyyy-MM-dd HH:mm:ss}]\t{log.Type}\t\t{log.Message}");

            MessageBox.Show($"Logs saved at {dirPath}");
        }

        public static void SaveBitmapTo(Bitmap bitmap, FolderName folderName, string fileName)
        {
            string dirPath = GetResultFolderPath(folderName);
            Directory.CreateDirectory(dirPath);

            string filePath = Path.Combine(dirPath, $"{fileName}.bmp");
            bitmap.Save(filePath, ImageFormat.Bmp);
        }

        private static string GetResultFolderPath(FolderName folderName)
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
                case FolderName.Log:
                    folderNameStr = "log";
                    break;
                case FolderName.ERROR:
                    folderNameStr = "_ERROR";
                    break;
                default:
                    MessageBox.Show("ERROR: Invalid Folder Name Chosen");
                    break;
            }

            return Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "_result",
                    folderNameStr,
                    TestIndexManager.Instance.ManagedDateTimeStr);
        }
    }
}
