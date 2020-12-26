using BlurryBitmap;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IBlurEffect blurEffect = new BlurEffect();

            var blurredImagePath = Path.Combine(AssemblyDirectory, "blurredCity.jpg");

            var assembly = Assembly.GetExecutingAssembly();
            using(var imgStream = assembly.GetManifestResourceStream("Test.city.jpg"))
            using(var bitmap = Image.FromStream(imgStream) as Bitmap)
            {
                await blurEffect.ApplyAsync(bitmap, 20, true);
                bitmap.Save(blurredImagePath);
            }

            Process.Start("cmd.exe ", $"/c {blurredImagePath}");
        }

        static string AssemblyDirectory => Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
    }
}
