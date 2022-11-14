using System.Runtime.Versioning;
using MapGenerator;

namespace Main
{
    [SupportedOSPlatform("windows")]
    static class Program
    {
        public static void Main(string[] args)
        
        {
            
            Generator.MakeMap();
            Console.WriteLine("Press [enter] to terminate");
            Console.ReadLine();
        }


    }
}