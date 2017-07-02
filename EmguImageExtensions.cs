using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Windows;
using System.IO;

namespace ImageManipulationExtensionMethods
{
    public static class EmguImageExtensions
    {
        public static Image<TColor, TDepth> ToOpenCVImage<TColor, TDepth>(this Bitmap bitmap)
            where TColor : struct, IColor
            where TDepth : new()
        {
            return new Image<TColor, TDepth>(bitmap);
        }

        public static System.Windows.Media.Imaging.BitmapSource ToBitmapSource(this IImage image)
        {
            var source = image.Bitmap.ToBitmapSource();
            return source;
        }
    }
}