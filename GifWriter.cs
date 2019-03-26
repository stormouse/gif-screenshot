
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace gif_screenshot
{
    /// <summary>
    /// Code learned from https://github.com/DataDink/Bumpkit
    /// also credit: https://stackoverflow.com/a/32810041
    /// </summary>
    public class GifWriter : IDisposable
    {
        const long SourceGlobalColorInfoPosition = 10;
        const long SourceImageBlockPosition = 789;

        private readonly BinaryWriter writer;
        private bool firstFrame = true;
        private readonly object _syncLock = new object();

        public int Width { get; set; }
        public int Height { get; set; }
        public int FrameDelay { get; set; }
        public int Repeat { get; set; }

        public GifWriter(Stream outStream, int frameDelay, int repeat)
        {
            if ( outStream == null )
                throw new ArgumentNullException( nameof( outStream ) );

            if ( frameDelay <= 0 )
                throw new ArgumentOutOfRangeException( nameof( frameDelay ) );

            if ( repeat < -1 )
                throw new ArgumentOutOfRangeException( nameof( repeat ) );

            writer = new BinaryWriter( outStream );
            this.FrameDelay = frameDelay;
            this.Repeat = repeat;
        }

        public GifWriter( string fileName, int frameDelay, int repeat )
            : this( new FileStream(
                      fileName,
                      FileMode.OpenOrCreate,
                      FileAccess.Write,
                      FileShare.Read ),
                  frameDelay,
                  repeat ) { }


        public void WriteFrame(Image image, int delay = 0)
        {
            lock (_syncLock)
            {
                using ( var gifStream = new MemoryStream() )
                {
                    image.Save( gifStream, ImageFormat.Gif );

                    if ( firstFrame )
                        InitHeader( gifStream, writer, image.Width, image.Height );

                    WriteGraphicControlBlock( gifStream, writer, delay == 0 ? FrameDelay : delay );
                    WriteImageBlock( gifStream, writer, !firstFrame, 0, 0, image.Width, image.Height );
                }
            }

            if ( firstFrame ) firstFrame = false;
        }

        private void InitHeader(Stream sourceGif, BinaryWriter writer, int width, int height)
        {
            writer.Write( "GIF".ToCharArray() );
            writer.Write( "89a".ToCharArray() );

            writer.Write( (short)( this.Width == 0 ? width : this.Width ) );
            writer.Write( (short)( this.Height == 0 ? height : this.Height ) );

            sourceGif.Position = SourceGlobalColorInfoPosition;
            writer.Write( (byte)sourceGif.ReadByte() );
            writer.Write( (byte)0 );
            writer.Write( (byte)0 );
            WriteColorTable( sourceGif, writer );

            if ( Repeat == -1 ) return;

            writer.Write( unchecked((short)0xff21) );       // Application Extension Block Identifier
            writer.Write( (byte)0x0b );                     // Application block Size
            writer.Write( "NETSCAPE2.0".ToCharArray() );    // Application Identifier
            writer.Write( (byte)3 );                        // Application block length
            writer.Write( (byte)1 );
            writer.Write( (short)Repeat );                  // Repeat count
            writer.Write( (byte)0 );                        // Terminator
            
        }

        private static void WriteColorTable(Stream sourceGif, BinaryWriter writer)
        {
            sourceGif.Position = 13;  // Locating the image color table
            byte[] colorTable = new byte[768];
            sourceGif.Read( colorTable, 0, colorTable.Length );
            writer.Write( colorTable, 0, colorTable.Length );
        }

        private static void WriteGraphicControlBlock(Stream sourceGif, BinaryWriter writer, int frameDelay)
        {
            sourceGif.Position = 781; // source GCE
            byte[] blockhead = new byte[8];
            sourceGif.Read( blockhead, 0, blockhead.Length );

            writer.Write( unchecked((short)0xf921) );               // Identifier
            writer.Write( (byte)0x04 );                             // Block size
            writer.Write( (byte)( blockhead[3] & 0xf7 | 0x08 ) );   // Disposal flag
            writer.Write( (short)( frameDelay / 10 ) );             // Frame delay
            writer.Write( blockhead[6] );                           // Transparent color index
            writer.Write( (byte)0 );                                // Terminator
        }

        private static void WriteImageBlock(
            Stream sourceGif, 
            BinaryWriter writer, 
            bool includeColorTable, 
            int x, int y, int width, int height)
        {
            sourceGif.Position = SourceImageBlockPosition;
            byte[] header = new byte[11];
            sourceGif.Read( header, 0, header.Length );

            writer.Write( header[0] );
            writer.Write( (short)x );
            writer.Write( (short)y );
            writer.Write( (short)width );
            writer.Write( (short)height );

            if(includeColorTable)
            {
                sourceGif.Position = SourceGlobalColorInfoPosition;
                writer.Write( (byte)( sourceGif.ReadByte() & 0x3f | 0x80 ) ); // enabling local color table
                WriteColorTable( sourceGif, writer );
            }
            else
            {
                writer.Write( (byte)( header[9] & 0x07 | 0x07 ) ); // disabling local color table
            }

            writer.Write( header[10] ); // LZW min code size

            sourceGif.Position = SourceImageBlockPosition + header.Length;

            var dataLength = sourceGif.ReadByte();
            while(dataLength > 0)
            {
                var imgData = new byte[dataLength];
                sourceGif.Read( imgData, 0, dataLength );
                writer.Write( (byte)dataLength );
                writer.Write( imgData, 0, dataLength );
                dataLength = sourceGif.ReadByte();
            }

            writer.Write( (byte)0 );
        }

        public void Dispose()
        {
            writer.Write( (byte)0x3b );  // file trailer
            writer.BaseStream.Dispose();
            writer.Dispose();
        }
    }
}
