using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Windows.Data;
using POGOProtos.Enums;

namespace Catchem.Extensions
{
    public class PokeDexImageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var pidQ = values[0] as PokemonId?;

            var seenQ = values[1] as bool?;

            var caughtQ = values[2] as bool?;
            if (pidQ == null || seenQ == null || caughtQ == null) return null;

            var seen = (bool) seenQ;
            var caught = (bool) caughtQ;
            var pid = (PokemonId) pidQ;

            if (!caught && seen)
            {
                return pid.ToInventoryBitmap().ToGrayscale().LoadBitmap();

            }
            return caught ? pid.ToInventorySource() : pid.ToInventoryBitmap().ToBlackout().LoadBitmap();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    internal static class ImageExtensions
    {
        internal static Bitmap ToGrayscale(this Bitmap source)
        {
            var colorMatrix = new ColorMatrix(
                new[]
                {
                    new[] {.3f, .3f, .3f, 0, 0}, // 30% Red
                    new[] {.59f, .59f, .59f, 0, 0}, // 59% Green
                    new[] {.11f, .11f, .11f, 0, 0}, // 11% Blue
                    new[] {0, 0, 0, 1f, 0}, // Alpha scales to 1
                    new[] {0, 0, 0, 0, 1f} // W is always 1
                });
            var attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);
            var newBitmap = new Bitmap(source.Width, source.Height);
            // Create a blank bitmap the same size as original and draw
            // the source image at it using the grayscale color matrix.    
            using (var g = Graphics.FromImage(newBitmap))
            {
                g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height),
                   0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            }
            return newBitmap;
        }

        internal static Bitmap ToBlackout(this Bitmap source)
        {
            var colorMatrix = new ColorMatrix(
                new[]
                {
                    new[] {0, 0, 0, 0, 0f}, // 100% black
                    new[] {0, 0, 0, 0, 0f}, // 100% black
                    new[] {0, 0, 0, 0, 0f}, // 100% black
                    new[] {0, 0, 0, 1f, 0}, // Alpha scales to 1
                    new[] {0, 0, 0, 0, 1f} // W is always 1
                });
            var attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);
            var newBitmap = new Bitmap(source.Width, source.Height);
            // Create a blank bitmap the same size as original and draw
            // the source image at it using the black color matrix.    
            using (var g = Graphics.FromImage(newBitmap))
            {
                g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height),
                   0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            }
            return newBitmap;
        }
    }
    
}
