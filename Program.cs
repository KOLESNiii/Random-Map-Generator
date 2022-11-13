using System;
using System.Drawing;
using System.Drawing.Imaging;
using SimplexNoise;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MapGenerator
{
    class Point
    {
        public Point(int xin, int yin)
        {
            x = xin;
            y = yin;
        }
        public int x;
        public int y;
        public int value = 0;
        private int r;
        public int R
        {
            get {return r;}
            set {
                if (value < 0)
                {
                    r = 0;
                }
                else if (value > 255)
                {
                    r = 255;
                }
                else
                {
                    r = value;
                }
            }
        }
        private int g;
        public int G
        {
            get {return g;}
            set {
                if (value < 0)
                {
                    g = 0;
                }
                else if (value > 255)
                {
                    g = 255;
                }
                else
                {
                    g = value;
                }
            }
        }
        private int b;
        public int B
        {
            get {return b;}
            set {
                if (value < 0)
                {
                    b = 0;
                }
                else if (value > 255)
                {
                    b = 255;
                }
                else
                {
                    b = value;
                }
            }
        }

        public void SetRGB(int channel, int value)
        {
            if (channel == 0)
            {
                R = value;
            }
            else if (channel == 1)
            {
                G = value;
            }
            else if (channel == 2)
            {
                B = value;
            }
        }

        public void SetRGB(string channel, int value)
        {
            if (channel == "r")
            {
                R = value;
            }
            else if (channel == "g")
            {
                G = value;
            }
            else if (channel == "b")
            {
                B = value;
            }
        }

    }
    [SupportedOSPlatform("windows")]
    class MapGenerator
    {
        public static int limit;

        public static List<double> proportions = new List<double>();
        public static float persistence;
        public static float scale;
        public static int resolution;
        public static int width;
        public static int height;
        public static int numThreads;
        public static void Main(string[] args)
        
        {
            Random rand = new Random();
            width = 20000;
            height = 10000;
            resolution = 1;
            int randomInt = rand.Next(1, 7);
            int randomInt2 = rand.Next(45, 55);
            scale = 0.0007f; //bigger this number, the smaller the individual biome is
            persistence = 0.50f; //0.45-0.55
            limit = 1000;
            proportions.Add(0.71); //water
            proportions.Add(0.01); //beach
            proportions.Add(0.21); //forest/grassland
            proportions.Add(0.07);
            Console.WriteLine($"Seed: {Seed()}");
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            List<Point> pointList = GenerateEmptyArray();
            GenerateSimplexNoise(pointList);
            GenerateRGBValues(pointList);
            Bitmap bmp = GenerateBMP(pointList);
            SaveImage(bmp, "noiseMap.png");
            watch.Stop();
            Console.WriteLine($"Finished creating map\nTook {watch.ElapsedMilliseconds}ms to create random map\nPress [enter] to terminate");
            Console.ReadLine();
        }

        public static bool Validation(string? input, out int numericalValue)
        {
            return Int32.TryParse(input, out numericalValue);
        }
        public static bool Validation(string? input, out string value)
        {
            if (String.IsNullOrEmpty(input))
            {
                value = "";
                return true;
            }
            else 
            {
                value = input;
                return false;
            }
        }
        public static void SaveImage( Bitmap image, string path)
        {
            image.Save(path, ImageFormat.Png);
        }
        public static List<Point> GenerateEmptyArray()
        {
            Console.Write("Creating new point array...");
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            List<Point> array = new List<Point>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    array.Add(new Point(x, y));
                }
            }
            watch.Stop();
            Console.WriteLine($"Took {watch.ElapsedMilliseconds}ms to create point array");
            return array;
        }
        public static void GenerateSimplexNoise(List<Point> array)
        {
            Console.Write("Creating noise using SimplexNoise...");
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            Parallel.ForEach(array, point =>
            {
                point.value = (int)SimplexNoise.Interpolation.sumOctave(8, point.x, point.y, persistence, scale, 0, limit);

            });
            watch.Stop();
            Console.WriteLine($"Took {watch.ElapsedMilliseconds}ms to generate simplex noise");
            
        }
        public static void GenerateRGBValues(List<Point> array)
        {
            Console.WriteLine("Converting to array to RGB...");
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            Parallel.ForEach(array, point => GetRGBValue(point));
            watch.Stop();
            Console.WriteLine($"Took {watch.ElapsedMilliseconds}ms to convert array");

        }
        public static void GetRGBValue(Point point)
        {
            List<int> min = new List<int>{};
            List<int> max = new List<int>{};
            int amountWater = (int)(limit * proportions[0]);
            int amountBeach = (int)(limit * proportions[1]) + amountWater;
            int amountForest = (int)(limit * proportions[2]) + amountBeach;
            int amountMountain = limit;
            double adjustedRawValue;
            if (point.value <= amountWater) //water
            {
                min.AddRange(new int[] {0, 14, 36});
                max.AddRange(new int[] {0, 77, 201});
                adjustedRawValue = GetAdjustedRawValue(point.value, 0, amountWater);
            }
            else if (point.value <= amountBeach) //beach
            {
                min.AddRange(new int[] {192, 199, 0});
                max.AddRange(new int[] {255, 255, 0});
                adjustedRawValue = GetAdjustedRawValue(point.value, amountWater+1, amountBeach);
            }
            else if (point.value <= amountForest) //grassland/woodland
            {
                min.AddRange(new int[] {125, 279, 0});
                max.AddRange(new int[] {83, 179, 0});
                adjustedRawValue = GetAdjustedRawValue(point.value, amountBeach+1, amountForest);
            }
            else //mountain
            {
                min.AddRange(new int[] {150, 150, 150});
                max.AddRange(new int[] {50, 50, 50});
                adjustedRawValue = GetAdjustedRawValue(point.value, amountForest+1, amountMountain);
            }
            var channelValues = ConvertToRGB(adjustedRawValue, min, max);
            for (int i = 0; i < 3; i++)
            {
                point.SetRGB(i, channelValues[i]);
            }
        }
        public static double GetAdjustedRawValue(double valueRaw, int min, int max)
        {
            int range = max - min;
            double tempRawValue = valueRaw - min;
            return (tempRawValue/range);
        }
        public static List<int> ConvertToRGB(double valueRaw, List<int> min, List<int> max)
        {
            List<int> rgb = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                rgb.Add(Normalize(valueRaw, min[i], max[i]));
            }
            return rgb;
        }
        public static int Normalize(double valueRaw, int min, int max)
        {
            int range = max - min;
            double tempValue = valueRaw * range;
            tempValue += (max > min) ? min : max;
            return (int)tempValue;
        }
        public static Bitmap GenerateBMP(List<Point> array)
        {
            Bitmap bmp = new Bitmap(width*resolution, height*resolution);
            Console.WriteLine("Converting to Bitmap...");
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            int count = 0;
            using (var progress = new ProgressBar())
            {
                foreach (Point point in array)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        for (int y = 0; y < resolution; y++)
                        {
                            bmp.SetPixel(point.x*resolution + x, point.y*resolution + y, Color.FromArgb(point.R, point.G, point.B));
                        }
                    }
                    count += 1;
                    progress.Report((double) (count)/(width*height));
                }
            }
            watch.Stop();
            Console.WriteLine($"\nTook {watch.ElapsedMilliseconds}ms to convert to bitmap");
            return bmp;
        }
    
        public static Int32 Seed()
        {
            Console.WriteLine("Enter seed: ");
            string? input = Console.ReadLine();
            //string? input = "";
            string output;
            int numericalOutput;
            Int32 seed;
            if (Validation(input, out output))
            {
                seed = SimplexNoise.Noise.MakeSeededGenerator();
            }
            else if (Validation(output, out numericalOutput))
            {
                seed = SimplexNoise.Noise.MakeSeededGenerator(numericalOutput);
            }
            else {

                Int32 seedNumber = 0;
                foreach (char c in output)
                {
                    seedNumber += c-64;
                }
                seed = SimplexNoise.Noise.MakeSeededGenerator(seedNumber);
            }
            return seed;
        }
    }
}