using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Input;

namespace gif_screenshot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly CommandBinding closeToQuitOverlay;
        private readonly KeyBinding escapeToClose;
        
        public MainWindow()
        {
            InitializeComponent();
            
            closeToQuitOverlay = new CommandBinding( ApplicationCommands.Close, CommandBinding_Executed );
            escapeToClose = new KeyBinding( ApplicationCommands.Close, new KeyGesture( Key.Escape ) );
        }

        private void Screenshot_Click( object sender, RoutedEventArgs e )
        {
            ScreenCapture sc = new ScreenCapture();
            System.Drawing.Image img = sc.CaptureScreen();

            MemoryStream memoryStream = new MemoryStream();

            img.Save( memoryStream, ImageFormat.Bmp );
            memoryStream.Position = 0;

            BitmapImage imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.StreamSource = memoryStream;
            imageSource.EndInit();

            // this.imagePreview.Stretch = System.Windows.Media.Stretch.Fill;
            this.imagePreview.Source = imageSource;
        }
        
        private void SelectArea_Click( object sender, RoutedEventArgs e )
        {
            Window cropper = new CroppingOverlay( this );
            this.Hide();
            cropper.Show();
        }

        private void CommandBinding_Executed( object sender, ExecutedRoutedEventArgs e )
        {
            this.CommandBindings.Remove( closeToQuitOverlay );
            this.InputBindings.Remove( escapeToClose );
        }
        
    }
}
