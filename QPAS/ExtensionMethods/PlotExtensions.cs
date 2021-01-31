// -----------------------------------------------------------------------
// <copyright file="PlotExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using OxyPlot.Wpf;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace QPAS
{
    public static class PlotExtensions
    {
        public static void SaveAsPNG(this PlotView plot)
        {
            if (plot == null) return;

            string path;
            SaveFileDialog file = new SaveFileDialog();
            file.Filter = @"png files (*.png)|*.png";
            file.RestoreDirectory = true;

            if (file.ShowDialog() == DialogResult.OK)
            {
                path = file.FileName;
            }
            else
            {
                return;
            }

            BitmapSource bmp = plot.ToBitmap();
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                encoder.Save(stream);
            }
        }

        public static void CopyToClipboard(this PlotView plot)
        {
            if (plot == null) return;

            BitmapSource bmp = plot.ToBitmap();
            Clipboard.SetImage(bmp.ToBitmap());
        }
    }
}
