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

        private static ICompressionUnit compressionUnit;
        private static int cursorPositionLeftForProgress;
        private static int cursorPositionTopForProgress;
        private static int programResult = 0;

        private static int Main(string[] args)
        {
            try
            {
                Console.CursorVisible = false;
                Console.CancelKeyPress += OnConsoleCancelKeyPressed;

                Console.WriteLine("GZip Compression Utility.");
                Console.WriteLine();
                
                if (args.Length != 3)
                {
                    ShowHelp();
                    return 1;
                }

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

                // Считываем пути к исходному и выходному файлу
                var currentDirectory = Directory.GetCurrentDirectory();
                var inputFileName = Path.Combine(currentDirectory, args[1]);
                var ouitputFileName = Path.Combine(currentDirectory, args[2]);

                Console.WriteLine("Press Ctrl+C to cancel operation.");

                // Запоминаем положение курсора на консоли
                cursorPositionLeftForProgress = Console.CursorLeft;
                cursorPositionTopForProgress = Console.CursorTop;

                compressionUnit.ProgressChanged += OnProgressChanged;
                compressionUnit.Completed += OnCompleted;

                // Запускаем секундомер для отсчета времени
                stopWatch.Start();

                // Запускаем операцию
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
            finally
            {
                Console.CursorVisible = true;
            }
        }

        private static void OnConsoleCancelKeyPressed(object sender, ConsoleCancelEventArgs e)
        {
            if (compressionUnit != null)
            {
                compressionUnit.Cancel();
                autoResetEvent.WaitOne();
            }
        }

        private static void OnProgressChanged(object sender, ProgressChangedEventArgs args)
        {
            Console.SetCursorPosition(cursorPositionLeftForProgress, cursorPositionTopForProgress);
            Console.WriteLine(string.Format("{0:F3} %", args.ProgressPercentage * 100));
        }

        private static void OnCompleted(object sender, CompletedEventArgs args)
        {
            var elapsedTime = stopWatch.Elapsed;
            stopWatch.Stop();

            switch (args.Status)
            {
                case CompletionStatus.Successed:
                    Console.WriteLine("Operation completed successfully.");
                    Console.WriteLine(string.Format("Time spent: {0}.", elapsedTime));
                    Console.WriteLine(string.Format("Input file size: {0} bytes.", args.InputFileSize));
                    Console.WriteLine(string.Format("Output file size: {0} bytes.", args.OutputFileSize));
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
            Console.WriteLine("Compress file:");
            Console.WriteLine("GZipCompressor.exe compress [source file name] [archive file name]");
            Console.WriteLine("Decompress file:");
            Console.WriteLine("GZipCompressor.exe decompress [archive file name] [source file name]");
        }
    }
}
