using System.Drawing;
using System.Drawing.Imaging;

namespace MapGenerator
{
    static class Generator
    {
        static Generator()
        {
            width = 10000;
            height = 10000;
            resolution = 1;
            scale!.AddRange(new List<float> {0.0001f, 0.0007f, 0.007f}); //bigger this number, the smaller the individual biome is
            //scaleContinents = 0.0007f; //noice for islands
            persistence!.AddRange(new List<float> {0.45f, 0.50f, 0.55f}); //smaller number means smoother, larger is more jagged
            limit = 1000;
            proportions.AddRange(new List<double> {0.55, 0.015, 0.29, 0.145}); //water;beach;forest;mountains
            amounts.Add((int)(limit * proportions[0]));
            amounts.Add((int)(limit * proportions[1]) + amounts[0]);
            amounts.Add((int)(limit * proportions[2]) + amounts[1]);
            amounts.Add((int) limit);
            passTypes.AddRange(new List<string> {"continents", "medium land features", "small land features"});
            numPasses = passTypes.Count;
        }
        public static int limit;
        public static List<double> proportions = new List<double>();
        public static List<float> persistence;
        public static List<float> scale;
        public static int resolution;
        public static int Resolution
        {get{return resolution;} set{resolution = (value >= 0) ? value : 0;}}
        public static Int64 width;
        public static Int64 Width
        {get{return width;} set{width = (value >= 0) ? value : 0;} }
        public static Int64 height;
        public static Int64 Height
        {get{return height;} set{height = (value >= 0) ? value : 0;} }
        public static List<int> amounts = new List<int>();
        public static List<string> passTypes = new List<string>();
        public static int numPasses;

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
            /*List<Point> array = new List<Point>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    array.Add(new Point(x, y));
                }
            }*/
            Point[] tempArray = new Point[width*height];
            Parallel.For(0, width, x => Parallel.For(0, height, y =>
            {
                tempArray[x*height+y] = new Point(x,y);
            }));
            //List<Point> array = tempArray.ToList(); //29344ms
            List<Point> array = new List<Point>(tempArray); //12055ms
            watch.Stop();
            Console.WriteLine($"Took {watch.ElapsedMilliseconds}ms to create point array");
            return array;
        }
        public static void GenerateSimplexNoise(List<Point> array)
        {
            Console.WriteLine("Generating map using SimplexNoise...");
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            for (int i = 0; i < numPasses; i++)
            {
                GenerateFeatures(array, i);
            }
            watch.Stop();
            Console.WriteLine($"Took {watch.ElapsedMilliseconds}ms to generate map using SimplexNoise");
        }
        public static void GenerateFeatures(List<Point> array, int passNumber)
        {
            Console.Write($"Creating {passTypes[passNumber]} using SimplexNoise...");
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            Parallel.ForEach(array, point =>
            {
                point.value += (int)SimplexNoise.Interpolation.sumOctave(8, point.x, point.y, persistence[passNumber], scale[passNumber], 0, limit);

            });
            watch.Stop();
            Console.WriteLine($"Took {watch.ElapsedMilliseconds}ms to generate {passTypes[passNumber]} using SimplexNoise");
            
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
            point.value /= numPasses;
            List<int> min = new List<int>{};
            List<int> max = new List<int>{};
            double adjustedRawValue;
            if (point.value <= amounts[0]) //water
            {
                min.AddRange(new int[] {0, 14, 36});
                max.AddRange(new int[] {0, 77, 201});
                adjustedRawValue = GetAdjustedRawValue(point.value, 0, amounts[0]);
            }
            else if (point.value <= amounts[1]) //beach
            {
                min.AddRange(new int[] {192, 199, 0});
                max.AddRange(new int[] {255, 255, 0});
                adjustedRawValue = GetAdjustedRawValue(point.value, amounts[0]+1, amounts[1]);
            }
            else if (point.value <= amounts[2]) //grassland/woodland
            {
                min.AddRange(new int[] {125, 279, 0});
                max.AddRange(new int[] {83, 179, 0});
                adjustedRawValue = GetAdjustedRawValue(point.value, amounts[1]+1, amounts[2]);
            }
            else //mountain
            {
                min.AddRange(new int[] {150, 150, 150});
                max.AddRange(new int[] {50, 50, 50});
                adjustedRawValue = GetAdjustedRawValue(point.value, amounts[2]+1, amounts[3]);
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
            Bitmap bmp = new Bitmap((int)width*resolution, (int)height*resolution);
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
                            bmp.SetPixel((int)point.x*resolution + x, (int)point.y*resolution + y, Color.FromArgb(point.R, point.G, point.B));
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
        public static Int32 NewSeed()
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