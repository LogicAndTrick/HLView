using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HLView.Graphics
{
    public static class ImageUtilities
    {
        public static Bitmap CreateBitmap(int width, int height, byte[] data, byte[] palette, bool lastTextureIsTransparent)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

            // Set palette
            var pal = bmp.Palette;
            for (var j = 0; j <= byte.MaxValue; j++)
            {
                var k = j * 3;
                pal.Entries[j] = Color.FromArgb(255, palette[k], palette[k + 1], palette[k + 2]);
            }

            if (lastTextureIsTransparent)
            {
                pal.Entries[pal.Entries.Length - 1] = Color.Transparent;
            }
            bmp.Palette = pal;

            // Write entries
            var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bmp.PixelFormat);
            Marshal.Copy(data, 0, bmpData.Scan0, data.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }
    }
}
