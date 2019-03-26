using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace gif_screenshot
{
    internal static class ScreenshotUtil
    {
        private static ScreenCapture sc = new ScreenCapture();

        public static Image GetScreenCaptureImage()
        {
            return sc.CaptureScreen();
        }

        public static ImageSource GetScreenshotAsImageSource()
        {
            System.Drawing.Image img = sc.CaptureScreen();

            MemoryStream memoryStream = new MemoryStream();

            img.Save( memoryStream, ImageFormat.Bmp );
            memoryStream.Position = 0;

            BitmapImage imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.StreamSource = memoryStream;
            imageSource.EndInit();

            return imageSource;
        }
        
    }
}
