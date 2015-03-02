using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GZipCompressor
{
    internal class Program
    {
        private static readonly Stopwatch stopWatch = new Stopwatch();
        private static readonly AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        private static readonly object consoleLock = new object();

        private static ICompressionUnit compressionUnit;
        private static int cursorPositionLeftForProgress;
        private static int cursorPositionTopForProgress;
        private static int programResult = 0;

        private static int Main(string[] args)
        {
            try
            {
                Console.CursorVisible = false;
                Console.WriteLine("GZip Compression Utility.");
                if (args.Length != 3)
                {
                    ShowHelp();
                    return 1;
                }

                var currentDirectory = Directory.GetCurrentDirectory();
                var inputFileName = Path.Combine(currentDirectory, args[1]);
                var ouitputFileName = Path.Combine(currentDirectory, args[2]);

                switch (args[0])
                {
                    case "compress":
                        compressionUnit = new Compressor();
                        Console.WriteLine("Compression started.");
                        break;
                    case "decompress":
                        compressionUnit = new Decompressor();
                        Console.WriteLine("Decompression started.");
                        break;
                    default:
                        ShowHelp();
                        return 1;
                }

                cursorPositionLeftForProgress = Console.CursorLeft;
                cursorPositionTopForProgress = Console.CursorTop;

                Console.CancelKeyPress += OnConsoleCancelKeyPressed;

                compressionUnit.ProgressChanged += OnProgressChanged;
                compressionUnit.Completed += OnCompressorCompleted;

                stopWatch.Start();
                compressionUnit.Execute(inputFileName, ouitputFileName);

                // Блокируем основной поток приложения до завершения операции.
                autoResetEvent.WaitOne();
                
                return programResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }
        }

        private static void OnConsoleCancelKeyPressed(object sender, ConsoleCancelEventArgs e)
        {
            if (compressionUnit != null)
            {
                compressionUnit.Cancel();
            }
        }

        private static void OnProgressChanged(object sender, ProgressChangedEventArgs args)
        {
            lock (consoleLock)
            {
                Console.SetCursorPosition(cursorPositionLeftForProgress, cursorPositionTopForProgress);
                Console.WriteLine(string.Format("{0:F3} %", args.ProgressPercentage * 100));
            }
        }

        private static void OnCompressorCompleted(object sender, CompletedEventArgs args)
        {
            var elapsedTime = stopWatch.Elapsed;
            stopWatch.Stop();

            switch (args.Status)
            {
                case CompletionStatus.Successed:
                    Console.WriteLine("Operation completed successfully.");
                    Console.WriteLine(string.Format(@"Time spent: {0}.", elapsedTime));
                    programResult = 0;
                    break;
                case CompletionStatus.Cancelled:
                    Console.WriteLine("Operation has been canceled.");
                    programResult = 1;
                    break;
                case CompletionStatus.Faulted:
                    Console.WriteLine("Operation failed.");
                    foreach (var exception in args.Exceptions)
                    {
                        Console.WriteLine(exception.Message);
                    }
                    programResult = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            autoResetEvent.Set();
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Compress file: Compressor.exe compress [source file name] [archive file name].");
            Console.WriteLine("Decompress file: Compressor.exe decompress [archive file name] [source file name].");
        }
    }
}
