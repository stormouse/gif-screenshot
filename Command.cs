using System.Windows.Input;

namespace gif_screenshot
{
    internal static class Command
    {
        public static readonly RoutedUICommand StartRecordingCommand = new RoutedUICommand( "Start Recording", "Start Recording", typeof( CroppingOverlay ) );
        public static readonly RoutedUICommand StopRecordingCommand = new RoutedUICommand( "Stop Recording", "Stop Recording", typeof( CroppingOverlay ) );
    }
}
