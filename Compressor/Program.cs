using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace Compressor
{
    internal class Program
    {
        private static Stopwatch stopWatch = new Stopwatch();

        private static void Main(string[] args)
        {
            string inputPath = @"C:\BevzOD\Video\Stand.Up.36.SATRip.avi";
            string outputPath = @"C:\BevzOD\Video\Stand.Up.36.SATRip.avi.gz";
            string decompessedPath = @"C:\BevzOD\Video\Stand.Up.36.SATRip2.avi";

            stopWatch.Start();

            var compressor = new Decompressor();
            //var compressor = new Compressor();
            compressor.ThreadsCount = 5;
            compressor.ProgressChanged += OnProgressChanged;
            compressor.Completed += OnCompressorCompleted;
            compressor.Execute(outputPath, decompessedPath);
            //compressor.Execute(inputPath, outputPath);

            Thread.Sleep(TimeSpan.FromMinutes(1).Milliseconds);
            
            //compressor.Execute(outputPath, decompessedPath);

            //stopWatch.Start();

            //var decompressor = new Decompressor();
            //decompressor.Decompress(outputPath, decompessedPath);
            //var t2 = stopWatch.Elapsed;
            //stopWatch.Stop();
        }

        private static void OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Console.WriteLine(string.Format("{0:F3} %", e.ProgressPercentage * 100));
        }

        private static void OnCompressorCompleted(object sender, CompletedEventArgs e)
        {
            var t1 = stopWatch.Elapsed;
            stopWatch.Stop();
        }
    }
}
