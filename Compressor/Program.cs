using System.Diagnostics;
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

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var compressor = new Compressor();

            compressor.Compress(inputPath, outputPath);

            var t1 = stopWatch.Elapsed;
            stopWatch.Reset();
            stopWatch.Start();
            compressor.Decompress(outputPath, decompessedPath);
            var t2 = stopWatch.Elapsed;
            stopWatch.Stop();
        }
    }
}
