using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Compressor
{
    public abstract class CompressionBase
    {
        private const int DEFAULT_THREADS_COUNT = 4;


        protected Thread readInputStreamThread;
        protected Thread writeOutputStreamThread;
        protected string inputPath;
        protected string outputPath;
        protected volatile bool cancellationPending;
        protected readonly OrderedQueue<byte[]> bufferQueue = new OrderedQueue<byte[]>();
        protected readonly List<Exception> innerExceptions = new List<Exception>();
        protected readonly ThreadScheduler threadScheduler = new ThreadScheduler(4);


        protected long inputStreamLength;
        protected long readenBytesCount;
        protected int totalBuffersCount;
        protected int compressedBuffersCount;
        protected int writtenBuffersCount;

        public CompressionBase()
        {
            readInputStreamThread = new Thread(ReadInputStream);
            writeOutputStreamThread = new Thread(WriteOutputStream);

            ThreadsCount = DEFAULT_THREADS_COUNT;
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        public event EventHandler<CompletedEventArgs> Completed;

        public int ThreadsCount { get; set; }

        public void Execute(string inputPath, string outputPath)
        {
            this.inputPath = inputPath;
            this.outputPath = outputPath;

            threadScheduler.MaxThreads = ThreadsCount;

            readInputStreamThread.Start();
            writeOutputStreamThread.Start();
        }

        protected virtual void ReadInputStream()
        {
            try
            {
                Debug.WriteLine("Read input stream thread with id " + Thread.CurrentThread.ManagedThreadId + " was started .");

                long[] blockIndexes;

                using (var inputStream = File.OpenRead(inputPath))
                {
                    inputStreamLength = inputStream.Length;
                    readenBytesCount = 0;
                    blockIndexes = CalculateBlockIndexes(inputStream);
                    totalBuffersCount = blockIndexes.Length;
                }

                for (int i = 0; i < blockIndexes.Length; i++)
                {
                    var blockIndex = i;

                    long blockLength = i < blockIndexes.Length - 1
                        ? blockIndexes[i + 1] - blockIndexes[i]
                        : inputStreamLength - blockIndexes[i];

                    threadScheduler.Enqueue(() => { TransformStreamBuffer(blockIndexes[blockIndex], (int)blockLength, blockIndex); });
                }
            }
            catch (Exception ex)
            {
                innerExceptions.Add(ex);

                Cancel();
            }
        }

        protected virtual void WriteOutputStream()
        {
            try
            {
                Debug.WriteLine("Write output stream thread was started " + Thread.CurrentThread.ManagedThreadId + " was started .");

                using (var outputStream = File.OpenWrite(outputPath))
                {
                    while (readInputStreamThread.IsAlive || 
                        bufferQueue.Count > 0 || 
                        threadScheduler.CurrentThreadsCount > 0 || 
                        readenBytesCount < inputStreamLength ||
                        compressedBuffersCount < totalBuffersCount)
                    {
                        if (cancellationPending)
                            break;

                        if (bufferQueue.Count > 0)
                        {
                            byte[] buffer;
                            if (bufferQueue.TryDequeue(out buffer))
                            {
                                outputStream.Write(buffer, 0, buffer.Length);
                                outputStream.Flush();

                                if (writtenBuffersCount != bufferQueue.CurrentOrder - 1)
                                    throw new Exception("Not ordered writing to the output file.");

                                writtenBuffersCount = bufferQueue.CurrentOrder;

                                // Размер буфера превышает ограничение сборщика мусора 8 Кб, 
                                // необходимо вручную очистить данные буфера из Large Object Heap 
                                GC.Collect();

                                ReportProgress();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                innerExceptions.Add(ex);
            }

            ReportCompletion();
        }

        protected abstract long[] CalculateBlockIndexes(Stream inputStream);

        //protected abstract long GetNextBlockIndex(Stream inputStream);

        protected abstract void TransformStreamBuffer(long streamStartIndex, int blockLength, int blockOrder);

        public void Cancel()
        {
            cancellationPending = true;
        }

        private void ReportCompletion()
        {
            var completedHandler = Completed;
            if (completedHandler != null)
            {
                CompletedEventArgs eventArgs = null;

                if (innerExceptions.Count > 0)
                {
                    eventArgs = CompletedEventArgs.Fault(innerExceptions);
                }
                else if (cancellationPending)
                {
                    eventArgs = CompletedEventArgs.Cancell();
                }
                else
                {
                    eventArgs = CompletedEventArgs.Success();
                }

                completedHandler.Invoke(this, eventArgs);
            }
        }

        protected void ReportProgress()
        {
            var progressChangedHandler = ProgressChanged;
            if (progressChangedHandler != null)
            {
                var readInputStreamPercentage = (double)readenBytesCount / inputStreamLength;
                var compressionPercentage = (double)compressedBuffersCount / totalBuffersCount;
                var writeOutputStreamPercentage = (double) writtenBuffersCount/totalBuffersCount;
                var processPercentage = (readInputStreamPercentage + compressionPercentage + writeOutputStreamPercentage) / 3;

                progressChangedHandler.Invoke(this, new ProgressChangedEventArgs(processPercentage));
            }
        }
    }
}