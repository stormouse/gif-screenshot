using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace gif_screenshot
{
    /// <summary>
    /// Interaction logic for CroppingOverlay.xaml
    /// </summary>
    public partial class CroppingOverlay : Window
    {
        private readonly Window parentWindow;

        private readonly CommandBinding startRecordingCommand;
        private readonly KeyBinding sToStartRecording;

        private readonly CommandBinding stopRecordingCommand;
        private readonly KeyBinding sToStopRecording;

        private readonly CommandBinding closeToQuitOverlay;
        private readonly KeyBinding escapeToClose;

        private Timer screenshotTimer;
        private GifWriter gifWriter;
        private MemoryStream memoryStream;
        private Rectangle cropRect;
        private System.Windows.Shapes.Rectangle drawRect;
        private Canvas canvas;
        private int sx, sy, tx, ty;

        private bool mouseIsDown;
        
        public CroppingOverlay( Window parent )
        {
            this.parentWindow = parent; 
            InitializeComponent();

            this.closeToQuitOverlay = new CommandBinding( ApplicationCommands.Close, CommandBinding_Executed );
            this.startRecordingCommand = new CommandBinding( Command.StartRecordingCommand, StartRecording );
            this.stopRecordingCommand = new CommandBinding( Command.StopRecordingCommand, StopRecording );

            this.escapeToClose = new KeyBinding( ApplicationCommands.Close, Key.Escape, ModifierKeys.None );
            this.sToStartRecording = new KeyBinding()
            {
                Command = Command.StartRecordingCommand,
                Key = Key.S
            };

            this.sToStopRecording = new KeyBinding()
            {
                Command = Command.StopRecordingCommand,
                Key = Key.S
            };

            this.CommandBindings.Add( closeToQuitOverlay );
            this.CommandBindings.Add( startRecordingCommand );
            this.CommandBindings.Add( stopRecordingCommand );

            this.InputBindings.Add( escapeToClose );
            this.InputBindings.Add( sToStartRecording );

            this.AllowsTransparency = true;
            this.Background = new SolidColorBrush( Colors.White ) { Opacity = 0.0 };

            canvas = new Canvas
            {
                Background = new SolidColorBrush( Colors.DarkGray ) { Opacity = 0.4 },
            };
            canvas.MouseMove += OnMouseMove;
            canvas.MouseDown += OnMouseDown;
            canvas.MouseUp += OnMouseUp;
            
            this.Content = canvas;

            mouseIsDown = false;
        }

        private void OnMouseDown(object sender, MouseEventArgs args)
        {
            Trace.WriteLine( "MouseDown" );
            mouseIsDown = true;
            canvas.Background = new SolidColorBrush( Colors.DarkGray ) { Opacity = 0.4 };
            sx = (int)args.GetPosition( this ).X;
            sy = (int)args.GetPosition( this ).Y;
        }

        private void OnMouseUp(object sender, MouseEventArgs args)
        {
            Trace.WriteLine( "MouseUp" );
            canvas.Background = new SolidColorBrush( Colors.White ) { Opacity = 0.01 };
            mouseIsDown = false;
        }

        private void OnMouseMove(object sender, MouseEventArgs args)
        {
            if(mouseIsDown)
            {
                tx = (int)args.GetPosition( this ).X;
                ty = (int)args.GetPosition( this ).Y;
                UpdateCropArea( sx, sy, tx, ty );
            }
        }


        private void UpdateCropArea(int x1, int y1, int x2, int y2)
        {
            int max_x = Math.Max( x1, x2 );
            int max_y = Math.Max( y1, y2 );
            int min_x = Math.Min( x1, x2 );
            int min_y = Math.Min( y1, y2 );

            if(drawRect != null)
            {
                if ( canvas.Children.IndexOf( drawRect ) != -1 )
                    canvas.Children.Remove( drawRect );
            }

            drawRect = new System.Windows.Shapes.Rectangle();
            drawRect.Stroke = new SolidColorBrush( Colors.LawnGreen );
            drawRect.StrokeThickness = 1.0;
            drawRect.Width = max_x - min_x;
            drawRect.Height = max_y - min_y;
            drawRect.IsHitTestVisible = false;

            canvas.Children.Add( drawRect );
            Canvas.SetTop( drawRect, min_y );
            Canvas.SetLeft( drawRect, min_x );

            cropRect = new Rectangle( min_x + 1, min_y + 1, max_x - min_x - 2, max_y - min_y - 2 );
        }
        

        private void StartRecording(object sender, ExecutedRoutedEventArgs args)
        {
            this.InputBindings.Remove( this.sToStartRecording );
            this.InputBindings.Add( this.sToStopRecording );
            this.memoryStream = new MemoryStream();
            this.gifWriter = new GifWriter( this.memoryStream, 50, 0 );

            StartTimedRecording();
        }

        private void StartTimedRecording()
        {
            screenshotTimer = new Timer( 50.0 );
            screenshotTimer.Elapsed += RecordTimer_Tick;
            screenshotTimer.AutoReset = true;
            screenshotTimer.Start();
        }

        private void RecordTimer_Tick(object sender, EventArgs args)
        {
            Bitmap src = new Bitmap( ScreenshotUtil.GetScreenCaptureImage() );
            Bitmap dst = src.Clone( cropRect, src.PixelFormat );
            
            gifWriter.WriteFrame( dst );
        }

        private void StopRecording(object sender, ExecutedRoutedEventArgs args)
        {
            screenshotTimer.Stop();

            string dir = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );
            string filepath = Path.Combine( dir, "temp.gif" );
            
            using ( FileStream fs = new FileStream( filepath , FileMode.Create ) )
            {
                byte[] data = memoryStream.GetBuffer();
                fs.Write( data, 0, data.Length );
            }

            this.Close( true );
        }

        private void Close(bool useCommand)
        {
            if ( useCommand ) CommandBinding_Executed( this, null );
            else this.Close();
        }
        
        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            this.CommandBindings.Remove( closeToQuitOverlay );
            this.InputBindings.Remove( escapeToClose );

            if(screenshotTimer != null && screenshotTimer.Enabled )
            {
                screenshotTimer.Stop();
            }

            this.parentWindow.Show();
            this.Close();
        }


    }
}
