using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Diagnostics;
using System.IO;
using ImageMagick;

namespace MapGenerator
{
    [SupportedOSPlatform("windows")]
    static class Generator
    {
        static Generator()
        {
            bool correct = false;
            while (!correct)
            {
                Console.WriteLine("Input width of whole image (int)");
                Width = Validation(); //more than 10000 either direction is breaking (for 16gb ram anyway!)
                Console.WriteLine("Input height of whole image (int)");
                Height = Validation();
                Console.WriteLine("Enter target pixels per block of map (1 or 2 recommended) (int)");
                Resolution = Validation();
                if ((width * height * resolution * resolution) > 700000000)
                {
                    Console.WriteLine("The product of the width, height and resolution squared must be less than 700 million");
                }
                else if (width * resolution > 65500)
                {
                    Console.WriteLine("The product of the width and resolution must be less than 65500");
                }
                else if (height * resolution > 65500)
                {
                    Console.WriteLine("The product of the height and resolution must be less than 65500");
                }
                else
                {
                    correct = true;
                }
            }
            int hcf = 1;
            for (int i = Math.Min(width, height); i > 0; i--)
            {
                if(width%i == 0 && height%i == 0)
                {
                    hcf = i;
                    if (hcf <= 5000)
                    {
                        break;
                    }
                }
            }
            combinedImages.AddRange(new List<int> {width/hcf,height/hcf}); 
            width = hcf;
            height = hcf;
            scale.AddRange(new List<float> {0.0001f, 0.0007f, 0.001f}); //bigger this number, the smaller the individual biome is
            //scaleContinents = 0.0007f; //noice for islands
            persistence.AddRange(new List<float> {0.45f, 0.50f, 0.50f}); //smaller number means smoother, larger is more jagged
            landformMultipliers.AddRange(new List<double> {1.0, 0.5, 0.2});
            limit = 1000;
            proportions.AddRange(new List<double> {0.52, 0.015, 0.20, 0.265}); //water;beach;forest;mountains
            amounts.Add((int)(limit * proportions[0]));
            amounts.Add((int)(limit * proportions[1]) + amounts[0]);
            amounts.Add((int)(limit * proportions[2]) + amounts[1]);
            amounts.Add((int) limit);
            passTypes.AddRange(new List<string> {"continents", "medium land features", "small land features"});
            numPasses = passTypes.Count;
            maxPasses = 3; //testing
            

        }
        public static int limit;
        public static List<double> proportions = new List<double>();
        public static List<float> persistence = new List<float>();
        public static List<float> scale = new List<float>();
        public static List<double> landformMultipliers = new List<double>();
        public static List<int> combinedImages = new List<int>();
        public static int imageCount = 0;
        public static int resolution;
        public static int Resolution
        {get{return resolution;} set{resolution = (value >= 0) ? value : 0;}}
        public static int width;
        public static int Width
        {get{return width;} set{width = (value >= 0) ? value : 0;} }
        public static int height;
        public static int Height
        {get{return height;} set{height = (value >= 0) ? value : 0;} }
        public static List<int> amounts = new List<int>();
        public static List<string> passTypes = new List<string>();
        public static int numPasses;
        public static int maxPasses; //for testing
        public static string imageFolderPath = "";
        private static List<Img> imageCoords = new List<Img>();

        public static void MakeMap()
        {
            Console.WriteLine($"Seed: {NewSeed()}");
            MakeEmptyImageFolder();
            var watch = new Stopwatch();
            watch.Start();
            long sumTimes = 0;
            for (int x = 0; x < combinedImages[0]*width*resolution; x+= width*resolution)
            {
                for (int y = 0; y < combinedImages[1]*height*resolution; y+= height*resolution)
                {
                    Console.WriteLine($"Generating map {imageCount+1}/{combinedImages[0]*combinedImages[1]}...");
                    var watchSmall = new Stopwatch();
                    watchSmall.Start();
                    var pointList = GenerateEmptyArray();
                    GenerateSimplexNoise(pointList, x/resolution, y/resolution);
                    GenerateRGBValues(pointList);
                    Bitmap bmp = GenerateBMP(pointList);
                    SaveImage(bmp, $"noiseMap-{imageCount}");
                    imageCount ++;
                    watchSmall.Stop();
                    imageCoords.Add(new Img(x, y, imageCount-1));
                    Console.WriteLine($"X:{x}, Y:{y}, IMGCOUNT:{imageCount-1}");
                    sumTimes += watchSmall.ElapsedMilliseconds;
                    var avgTimeMS = sumTimes/imageCount;
                    var ETAms = avgTimeMS * (combinedImages[0]*combinedImages[1] - imageCount); 
                    var ETAs = (int)ETAms / 1000;
                    var ETAm = ETAs / 60;
                    ETAs %= 60;
                    Console.WriteLine($"Generated map {imageCount}/{combinedImages[0]*combinedImages[1]}...\n{imageCount*100/(combinedImages[0]*combinedImages[1])}% complete...ETA {ETAm} min {ETAs} s");

                }
            }
            Console.WriteLine("Joining all images...");
            ConvertToOneLarge();
            //ClearPartImages();
            watch.Stop();
            Console.WriteLine($"Finished creating map\nTook {watch.ElapsedMilliseconds}ms to create all maps\n");
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
                return false;
            }
            else 
            {
                value = input;
                return true;
            }
        }
        public static int Validation()
        {
            int numericalOutput;
            string? input;
            do
            {
                input = Console.ReadLine();
            } while (!Validation(input, out numericalOutput));
            return numericalOutput;
        }
        public static void SaveImage( Bitmap image, string imageName)
        {
            string path = Path.Combine(imageFolderPath, $"{imageName}.png");
            image.Save(path, ImageFormat.Png);
        }
        private static void MakeEmptyImageFolder()
        {
            string currentDir = Directory.GetCurrentDirectory();
            imageFolderPath = Path.Combine(currentDir, "images\\");
            Directory.CreateDirectory(imageFolderPath);
            Array.ForEach(Directory.GetFiles(imageFolderPath, "*.png", SearchOption.TopDirectoryOnly), delegate(string path) {File.Delete(path);});
        }
        public static void LoadImages()
        {
            Console.WriteLine("Combining all images...");
            var watch = new Stopwatch();
            watch.Start();
            string[] imagePaths = Directory.GetFiles(imageFolderPath, "*.png", SearchOption.TopDirectoryOnly);
            Parallel.ForEach(imageCoords, coord =>
            {
                var fileName = imagePaths.Where(path => 
                {
                    int lastIndex = path.Length-5;
                    int firstIndex = lastIndex;
                    int num = 0;
                    for (int i = lastIndex-1; i >= 0; i--)
                    {
                        if (path.Substring(i,1) == "-")
                        {
                            break;
                        }
                        else
                        {
                            firstIndex--;
                        }
                    }
                    Int32.TryParse(path.Substring(firstIndex, lastIndex-firstIndex+1), out num);
                    return num == coord.imageNum;
                }).ToList();
                if (fileName is null)
                {
                    throw new FileNotFoundException("No valid png file for generating one image, reload");
                }
                else if (fileName.Count == 0)
                {
                    throw new FileNotFoundException("No valid png file for generating one image, reload");
                }
                coord.image = Image.FromFile(fileName[0]);
            });
            var bmp = CombineImages();
            SaveImage(bmp, "finalImage");
            //ClearPartImages();
            watch.Stop();
            Console.WriteLine($"Combined {imagePaths.Length} files in {watch.ElapsedMilliseconds}ms");
        }
        public static void ConvertToOneLarge()
        {
            var watch = new Stopwatch();
            watch.Start();
            string[] imagePaths = Directory.GetFiles(imageFolderPath, "*.png", SearchOption.TopDirectoryOnly);
            using (Bitmap bmp = new Bitmap((int)(width*Resolution*combinedImages[0]), (int)(height*Resolution*combinedImages[1]), PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {   
                    int count = 0;
                    foreach (Img coord in imageCoords)
                    {   
                        var watchSmall = new Stopwatch();
                        watchSmall.Start();
                        var fileName = imagePaths.AsParallel().Where(path => 
                        {
                            int lastIndex = path.Length-5;
                            int firstIndex = lastIndex;
                            int num = 0;
                            for (int i = lastIndex-1; i >= 0; i--)
                            {
                                if (path.Substring(i,1) == "-")
                                {
                                    break;
                                }
                                else
                                {
                                    firstIndex--;
                                }
                            }
                            bool isNum = Int32.TryParse(path.Substring(firstIndex, lastIndex-firstIndex+1), out num);
                            return (num == coord.imageNum && isNum);
                            }).ToList();
                        if (fileName is null)
                        {
                            throw new FileNotFoundException("No valid png file for generating one image, reload");
                        }
                        else if (fileName.Count == 0)
                        {
                            throw new FileNotFoundException("No valid png file for generating one image, reload");
                        }
                        var image = Image.FromFile(fileName[0]);
                        g.DrawImage(image, coord.tl[0], coord.tl[1]);
                        watchSmall.Stop();
                        count ++;
                        Console.WriteLine($"Joined image {count}/{imageCoords.Count} in {watchSmall.ElapsedMilliseconds}ms, {(int)count*100/imageCoords.Count}% complete...");
                    }
                }
                watch.Stop();
                Console.WriteLine($"Finished combining all images in {watch.ElapsedMilliseconds}ms");
                string path = Path.Combine(imageFolderPath, "mainImage.png");
                Bitmap tempBitmap = bmp;
                tempBitmap.Save(path, ImageFormat.Png);
                //SaveImage(bmp, "MAINimage");
            }
        }
        public static void ClearPartImages()  //need to fix
        {
            Console.WriteLine("Deleting residual files...");
            var watch = new Stopwatch();
            watch.Start();
            var images = Directory.GetFiles(imageFolderPath, "*.png");
            int tempNum;
            List<string> pathsToDelete = new List<string>();
            foreach (string imagePath in images)
            {
                
                for (int i = 0; i < imagePath.Length; i++)
                {
                    Console.WriteLine(imagePath.Substring(i,1));
                    if (Int32.TryParse(imagePath.Substring(i,1), out tempNum))
                    {
                        pathsToDelete.Add(imagePath);
                    }
                }
            }
            foreach (string path in pathsToDelete)
            {
                Console.WriteLine(path);
                File.Delete(path);
            }
            watch.Stop();
            Console.WriteLine($"Took {watch.ElapsedMilliseconds}ms to delete residual files");
        }

        public static Bitmap CombineImages()
        {
            Bitmap bmp = new Bitmap((int)(width*Resolution*combinedImages[0]), (int)(height*Resolution*combinedImages[1]));
            Console.WriteLine("Combining all images...");
            int count = 0;
            var watch = new Stopwatch();
            watch.Start();
            using (Graphics g = Graphics.FromImage(bmp))
            {
                foreach (Img coord in imageCoords)
                {
                    var watchSmall = new Stopwatch();
                    watchSmall.Start();
                    g.DrawImage(coord.image, coord.tl[0], coord.tl[1]);
                    watchSmall.Stop();
                    count ++;
                    Console.WriteLine($"Finished combining image {coord.imageNum}...{(double) count / imageCoords.Count}% complete");
                }
            }
            watch.Stop();
            Console.WriteLine($"Took {watch.ElapsedMilliseconds} to combine all images");
            return bmp;
        }
        public static List<Point> GenerateEmptyArray()
        {
            //Console.Write("Creating new point array...");
            var watch = new Stopwatch();
            watch.Start();
            Point[] tempArray = new Point[width*height];
            Parallel.For(0, width, x => Parallel.For(0, height, y =>
            {
                tempArray[x*height+y] = new Point(x,y);
            }));
            //List<Point> array = tempArray.ToList(); //29344ms
            List<Point> array = new List<Point>(tempArray); //12055ms
            watch.Stop();
            //Console.WriteLine($"Took {watch.ElapsedMilliseconds}ms to create point array");
            return array;
        }
        public static void GenerateSimplexNoise(List<Point> array, int xOffset, int yOffset)
        {
            //Console.WriteLine("Generating map using SimplexNoise...");
            var watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < Math.Min(numPasses, maxPasses); i++)
            {
                GenerateFeatures(array, i, xOffset, yOffset);
            }
            watch.Stop();
            //Console.WriteLine($"Took {watch.ElapsedMilliseconds}ms to generate map using SimplexNoise");
            AdjustValueRange(array);
        }
        public static void GenerateFeatures(List<Point> array, int passNumber, int xOffset, int yOffset)
        {
            //Console.Write($"Creating {passTypes[passNumber]} using SimplexNoise...");
            var watch = new Stopwatch();
            watch.Start();
            Parallel.ForEach(array, point =>
            {
                point.value += (SimplexNoise.Interpolation.sumOctave(8, point.x+xOffset, point.y+yOffset, persistence[passNumber], scale[passNumber]) * landformMultipliers[passNumber]);

            });
            watch.Stop();
            //Console.WriteLine($"Took {watch.ElapsedMilliseconds}ms to generate {passTypes[passNumber]} using SimplexNoise");
            
        }
        public static void AdjustValueRange(List<Point> array)
        {
            //Console.Write($"Changing the range of point values...");
            var watch = new Stopwatch();
            watch.Start();
            Parallel.ForEach(array, point =>
            {
                double maxValue = landformMultipliers.Sum();
                point.value += maxValue;
                point.value /= (maxValue * 2);
                point.value *= limit;
            });
            watch.Stop();
            //Console.WriteLine($"Took {watch.ElapsedMilliseconds}ms to change ranges of point values");
        }
        public static void GenerateRGBValues(List<Point> array)
        {
            //Console.WriteLine("Converting to array to RGB...");
            var watch = new Stopwatch();
            watch.Start();
            Parallel.ForEach(array, point => GetRGBValue(point));
            watch.Stop();
            //Console.WriteLine($"Took {watch.ElapsedMilliseconds}ms to convert array");

        }
        public static void GetRGBValue(Point point)
        {
            //point.value /= (numPasses*((double)(2/3)));
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
            Bitmap bmp = new Bitmap((int)width*resolution, (int)height*resolution, PixelFormat.Format24bppRgb);
            //var m = new MagickFactory();
            
            Console.WriteLine("Converting to Bitmap...");
            var watch = new Stopwatch();
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
            MagickImage image = new MagickImage();
            watch.Stop();
            //Console.WriteLine($"Took {watch.ElapsedMilliseconds}ms to convert to bitmap");
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
            if (!Validation(input, out output))
            {
                seed = SimplexNoise.Noise.MakeSeededGenerator();
            }
            else if (Validation(output, out numericalOutput))
            {
                seed = SimplexNoise.Noise.MakeSeededGenerator(numericalOutput);
            }
            else {

                
                seed = SimplexNoise.Noise.MakeSeededGenerator(output);
            }
            return seed;
        }
    }
}