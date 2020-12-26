using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlurryBitmap
{
    public class BlurEffect : IBlurEffect
    {
        public Task ApplyAsync(Bitmap bitmap, int blurPixelRadius, bool fast = true)
        {
            if(bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));
            if(blurPixelRadius <= 0)
                throw new ArgumentException("Pixel radius must be positive", nameof(blurPixelRadius));
            if(blurPixelRadius >= bitmap.Width)
                throw new ArgumentException("Pixel radius must less than bitmap width", nameof(blurPixelRadius));
            if(blurPixelRadius >= bitmap.Height)
                throw new ArgumentException("Pixel radius must less than bitmap height", nameof(blurPixelRadius));

            if(fast)
                return Task.Factory.StartNew(() => ApplyFast(bitmap, blurPixelRadius), TaskCreationOptions.LongRunning);
            else
                return Task.Factory.StartNew(() => Apply(bitmap, blurPixelRadius), TaskCreationOptions.LongRunning);
        }


        void Apply(Bitmap originalBitmap, int blurPixelRadius)
        {
            unsafe
            {
                var width = originalBitmap.Width;
                var height = originalBitmap.Height;
                var workingRect = new Rectangle(0, 0, width, height);

                var originalBitmapData = originalBitmap.LockBits(workingRect, System.Drawing.Imaging.ImageLockMode.ReadWrite, originalBitmap.PixelFormat);
                var stride = originalBitmapData.Stride;
                var ptr = originalBitmapData.Scan0;


                var bytesCount = Math.Abs(stride) * height;
                byte[] blurredRgbValues = new byte[bytesCount];

                var coeffGrid = GetCircularCoeffGrid(blurPixelRadius);
                PrintCoeffGrid(coeffGrid);
                Parallel.For(0, height, BlurRow);
                //for(int row = 0; row < height; row++) BlurRow(row);
                


                Marshal.Copy(blurredRgbValues, 0, ptr, bytesCount);
                originalBitmap.UnlockBits(originalBitmapData);


                void BlurRow(int row)
                {
                    for(int column = 0; column < width; column++)
                    {
                        List<float> rPoints = new List<float>();
                        List<float> gPoints = new List<float>();
                        List<float> bPoints = new List<float>();

                        int coeffCnt = 0;
                        for(int dX = -blurPixelRadius; dX <= blurPixelRadius; dX++)
                        {
                            for(int dY = -blurPixelRadius; dY <= blurPixelRadius; dY++)
                            {
                                var coeff = coeffGrid[dX + blurPixelRadius, dY + blurPixelRadius];
                                
                                if(coeff)
                                {
                                    coeffCnt++;
                                    var x = column + dX;
                                    var y = row + dY;

                                    if(x >= 0 && y >= 0 && x < width && y < height)
                                    {
                                        var rIndex = (byte*)(ptr + y * stride + x * 3);
                                        var r = rIndex[0];
                                        var g = rIndex[1];
                                        var b = rIndex[2];

                                        rPoints.Add(r);
                                        gPoints.Add(g);
                                        bPoints.Add(b);
                                    }
                                }
                            }
                        }

                        var rAvg = (byte)rPoints.Average();
                        var gAvg = (byte)gPoints.Average();
                        var bAvg = (byte)bPoints.Average();

                        var destR = row * stride + column * 3;
                        blurredRgbValues[destR] = rAvg;
                        blurredRgbValues[destR + 1] = gAvg;
                        blurredRgbValues[destR + 2] = bAvg;
                    }
                }
            }
        }

        void ApplyFast(Bitmap originalBitmap, int blurPixelRadius)
        {
            unsafe
            {
                var width = originalBitmap.Width;
                var height = originalBitmap.Height;
                var workingRect = new Rectangle(0, 0, width, height);

                var originalBitmapData = originalBitmap.LockBits(workingRect, System.Drawing.Imaging.ImageLockMode.ReadWrite, originalBitmap.PixelFormat);
                var stride = originalBitmapData.Stride;
                var ptr = originalBitmapData.Scan0;

                var bytesCount = Math.Abs(stride) * height;
                byte[] blurredRgbValues = new byte[bytesCount];

                var blurExpansion = blurPixelRadius * 2 + 1;
                var blurPointsCount = Math.Pow(blurExpansion, 2);
                Parallel.For(0, height, BlurRow);
                //for(int row = 0; row < height; row++) BlurRow(row);

                Marshal.Copy(blurredRgbValues, 0, ptr, bytesCount);
                originalBitmap.UnlockBits(originalBitmapData);

                void BlurRow(int row)
                {
                    var minY = Math.Max(0, row - blurPixelRadius);
                    var maxY = Math.Min(row, row + blurPixelRadius);
                    var ver = maxY - minY;


                    for(int column = 0; column < width; column++)
                    {
                        float rAvgF = 0, gAvgF = 0, bAvgF = 0;
                        var minX = Math.Max(0, column - blurPixelRadius);
                        var maxX = Math.Min(width, column + blurPixelRadius);

                        var hor = maxX - minX;

                        var cnt = (float)(hor * ver);
                        
                        for(int x = minX; x < maxX; x++)
                        {
                            for(int y = minY; y < maxY; y++)
                            {
                                var rIndex = (byte*)(ptr + y * stride + x * 3);
                                var r = rIndex[0];
                                var g = rIndex[1];
                                var b = rIndex[2];

                                rAvgF += (r / cnt);
                                gAvgF += (g / cnt);
                                bAvgF += (b / cnt);
                            }
                        }

                        var rAvg = (byte)rAvgF;
                        var gAvg = (byte)gAvgF;
                        var bAvg = (byte)bAvgF;

                        var destR = row * stride + column * 3;
                        blurredRgbValues[destR] = rAvg;
                        blurredRgbValues[destR + 1] = gAvg;
                        blurredRgbValues[destR + 2] = bAvg;
                    }
                }
            }
        }

        bool[,] GetCircularCoeffGrid(int radius)
        {
            var size = radius * 2 + 1;
            var grid = new bool[size, size];

            for(int x = 0; x < size; x++)
            {
                for(int y = 0; y < size; y++)
                {
                    var diffX = x - radius;
                    var diffY = y - radius;

                    var distance = (float)Math.Sqrt(Math.Pow(diffX, 2) + Math.Pow(diffY, 2));


                    grid[x, y] = distance <= radius;
                }
            }

            return grid;
        }

        void PrintCoeffGrid(bool[,] grid)
        {
            for(int i = 0; i < grid.GetLength(0); i++)
            {
                for(int k = 0; k < grid.GetLength(1); k++)
                {
                    var item = grid[i, k];
                    Console.Write(item ? " *" : "  ");
                }

                Console.WriteLine();
            }
        }
    }
}
