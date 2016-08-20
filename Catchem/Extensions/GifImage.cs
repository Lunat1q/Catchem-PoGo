using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Catchem.Extensions
{

    internal class GifImage
    {
        private readonly Bitmap _bitmap;
        private BitmapSource _source;
        public System.Windows.Controls.Image Image;

        public GifImage(Bitmap bitmap)
        {
            _bitmap = bitmap;
            _source = GetSource();
            if (_source != null)
                Image = new System.Windows.Controls.Image { Source = GetSource() };
            ImageAnimator.Animate(bitmap, OnFrameChanged);
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private BitmapSource GetSource()
        {
            try
            {
                var handle = _bitmap.GetHbitmap();
                var res = Imaging.CreateBitmapSourceFromHBitmap(
                    handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                DeleteObject(handle);
                return res;
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        private void FrameUpdatedCallback()
        {
            ImageAnimator.UpdateFrames();
            _source?.Freeze();
            _source = GetSource();
            if (_source != null)
                Image.Source = _source;
            Image.InvalidateVisual();
        }

        private void OnFrameChanged(object sender, EventArgs e)
        {
            Image?.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                    new Action(FrameUpdatedCallback));
        }

        public void Dispose()
        {
            _bitmap.Dispose();
        }
    }
}
