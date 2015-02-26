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
            string inputPath = @"C:\BevzOD\Video\data.txt";
            string outputPath = @"C:\BevzOD\Video\data.txt.gz";
            string decompessedPath = @"C:\BevzOD\Video\data2.txt";

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var compressor = new Compressor();
            compressor.Compress(inputPath, outputPath);

            stopWatch.Reset();
            stopWatch.Start();

            var decompressor = new Decompressor();
            decompressor.Decompress(outputPath, decompessedPath);
            var t2 = stopWatch.Elapsed;
            stopWatch.Stop();
        }
    }
}
