using System;
using System.Drawing;
using System.Drawing.Imaging;
using SimplexNoise;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MapGenerator
{
    class RGB
    {
        public RGB(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }
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

    class Point
    {
        public Point(int xin, int yin, int valuein)
        {
            x = xin;
            y = yin;
            value = valuein;
        }
        public int x;
        public int y;
        public int value;
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
            width = 4000;
            height = 2000;
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
            double[,] valuesRaw = GenerateArray();
            RGB[,] RGBArray = GenerateRGBArray(valuesRaw);
            Bitmap bmp = GenerateBMP(RGBArray);

            SaveImage(bmp, "noiseMap.png");
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

        public static double[,] GenerateArray()
        {
            double[,] values = new double[width, height];
            Console.Write("Creating noise using SimplexNoise...");
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            Parallel.For(0, width, x => Parallel.For(0, height, y =>
            {
                values[x,y] = (int)SimplexNoise.Interpolation.sumOctave(8, x, y, persistence, scale, 0, limit);

            }));
            watch.Stop();
            Console.WriteLine($"Took {watch.ElapsedMilliseconds}ms to generate array");
            return values;
            
        }


        public static RGB[,] GenerateRGBArray(double[,] array)
        {
            RGB[,] RGBArray = new RGB[width, height];
            Console.WriteLine("Converting to RGB");
            using (var progress = new ProgressBar()) 
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        RGBArray[x,y] = GetRGBValue(array[x,y]);
                        progress.Report((double) (x*height+y)/(width*height));
                    }
                    
                }
            }
            Console.WriteLine("\n");
            return RGBArray;

        }

        public static RGB GetRGBValue(double valueRaw)
        {
            List<int> min = new List<int>{};
            List<int> max = new List<int>{};
            int amountWater = (int)(limit * proportions[0]);
            int amountBeach = (int)(limit * proportions[1]) + amountWater;
            int amountForest = (int)(limit * proportions[2]) + amountBeach;
            int amountMountain = limit;
            double adjustedRawValue;
            if (valueRaw <= amountWater) //water
            {
                min.AddRange(new int[] {0, 14, 36});
                max.AddRange(new int[] {0, 77, 201});
                adjustedRawValue = GetAdjustedRawValue(valueRaw, 0, amountWater);
            }
            else if (valueRaw <= amountBeach) //beach
            {
                min.AddRange(new int[] {192, 199, 0});
                max.AddRange(new int[] {255, 255, 0});
                adjustedRawValue = GetAdjustedRawValue(valueRaw, amountWater+1, amountBeach);
            }
            else if (valueRaw <= amountForest) //grassland/woodland
            {
                min.AddRange(new int[] {125, 279, 0});
                max.AddRange(new int[] {83, 179, 0});
                adjustedRawValue = GetAdjustedRawValue(valueRaw, amountBeach+1, amountForest);
            }
            else //mountain
            {
                min.AddRange(new int[] {150, 150, 150});
                max.AddRange(new int[] {50, 50, 50});
                adjustedRawValue = GetAdjustedRawValue(valueRaw, amountForest+1, amountMountain);
            }
            return ConvertToRGB(adjustedRawValue, min, max);
        }

        public static double GetAdjustedRawValue(double valueRaw, int min, int max)
        {
            int range = max - min;
            double tempRawValue = valueRaw - min;
            return (tempRawValue/range);
        }

        public static RGB ConvertToRGB(double valueRaw, List<int> min, List<int> max)
        {
            RGB rgb = new RGB(0,0,0);
            for (int i = 0; i < 3; i++)
            {
                rgb.SetRGB(i, Normalize(valueRaw, min[i], max[i]));
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
    
        public static Bitmap GenerateBMP(RGB[,] RGBArray)
        {
            int width = RGBArray.GetLength(0);
            int height = RGBArray.GetLength(1);
            Bitmap bmp = new Bitmap(width*resolution, height*resolution);
            Console.WriteLine("Converting to Bitmap");
            using (var progress = new ProgressBar())
            {
                for (int x = 0; x < width*resolution; x+= resolution)
                {
                    for (int y = 0; y < height*resolution; y+= resolution)
                    {
                        for (int ix = 0; ix < resolution; ix++)
                        {
                            for (int iy = 0; iy < resolution; iy++)
                            {
                                RGB pixel = RGBArray[x,y];
                                bmp.SetPixel(x + ix, y + iy, Color.FromArgb(pixel.R, pixel.G, pixel.B));
                            }
                            
                        }
                        progress.Report((double) (x*height*resolution+y)/(width*height*resolution*resolution));
                    }
                }
            }
            Console.WriteLine("\n");
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