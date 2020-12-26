[![Nuget](https://img.shields.io/nuget/v/BlurryBitmap)](https://www.nuget.org/packages/BlurryBitmap)

You blur Bitmap objects with specifing blur radius in pixels.


You can use ```BlurEffect``` class instance and call ```ApplyAsync(bitmap, radius)``` after the task is complete, the passed bitmap will be modified with blur effect applied.
The ```ApplyAsync``` method is running on different thread and using ```Parallel LINQ (PLINQ)``` to utilize all available resources from hardware.
```ApplyAsync``` has third additonal argument ```fast``` with default value to true wich controls the blurring process. If fast way is chosen the blur is calculated by the square 
shape which is not correct but it is 10x faster and the result is not very different from second value, which calculates blur by the circle which creates correct blur but it 
requires more resources and time.

Sample usage:
```cs
IBlurEffect blurEffect = new BlurEffect();
using(var bitmap = Image.FromFile("/path/img.jpg"))
{
    await blurEffect.ApplyAsync(bitmap, 20, true);
    bitmap.Save("/path/blurred_img.jpg");
}
```

**Original image**
![Original image](https://github.com/mister-giga/BlurryBitmap/blob/master/Media/city.jpg?raw=true)

**Blurred with ```fast=true```**
![Blurred with fast option](https://github.com/mister-giga/BlurryBitmap/blob/master/Media/blurredCity_fast.jpg?raw=true)

**Blurred with ```fast=false```**
![Blurred with slow option](https://github.com/mister-giga/BlurryBitmap/blob/master/Media/blurredCity_slow.jpg?raw=true)
