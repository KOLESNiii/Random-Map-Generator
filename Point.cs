
namespace MapGenerator
{
    class Point
    {
        public Point(long xin, long yin)
        {
            x = xin;
            y = yin;
        }
        public long x;
        public long y;
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
}