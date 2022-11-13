using System.Runtime.Versioning;
using MapGenerator;
using System.Drawing;

namespace Main
{
    [SupportedOSPlatform("windows")]
    static class Program
    {
        public static void Main(string[] args)
        
        {
            
            Console.WriteLine($"Seed: {Generator.NewSeed()}");
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            var pointList = Generator.GenerateEmptyArray();
            Generator.GenerateSimplexNoise(pointList);
            Generator.GenerateRGBValues(pointList);
            Bitmap bmp = Generator.GenerateBMP(pointList);
            Generator.SaveImage(bmp, "noiseMap.png");
            watch.Stop();
            Console.WriteLine($"Finished creating map\nTook {watch.ElapsedMilliseconds}ms to create random map\nPress [enter] to terminate");
            Console.ReadLine();
        }


    }
}