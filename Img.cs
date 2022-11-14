using System.Drawing;
namespace MapGenerator
{
    class Img
    {
        public Img(int TLx, int TLy, int imgNum)
        {
            tl.AddRange(new List<int> {TLx, TLy});  
            imageNum = imgNum;      
        }

        public Image image = new Bitmap(1,1);
        public List<int> tl = new List<int>();
        public int imageNum;
    }
}