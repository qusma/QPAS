using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace QPAS
{
    public static class BitmapSourceExtensions
    {
        public static Bitmap ToBitmap(this BitmapSource bitmapsource)
        {
            Bitmap bitmap;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }
    }
}
