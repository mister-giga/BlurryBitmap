using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

namespace BlurryBitmap
{
    public interface IBlurEffect
    {
        Task ApplyAsync(Bitmap bitmap, int blurPixelRadius, bool fast = true);
    }
}
