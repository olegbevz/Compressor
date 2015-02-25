using System.IO;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Compressor
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string inputPath = @"C:\BevzOD\Video\Stand.Up.36.SATRip.avi";
            string outputPath = @"C:\BevzOD\Video\Stand.Up.36.SATRip.avi.gz";
            string decompessedPath = @"C:\BevzOD\Video\Stand.Up.36.SATRip2.avi";

            var compressor = new Compressor();

            compressor.Compress(inputPath, outputPath);

            compressor.Decompress(outputPath, decompessedPath);
        }
    }
}
