using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Compressor
{
    public static class StreamExtensions
    {
        public static long CopyTo(this Stream sourceStream, Stream targetStream, int copyBufferSize = 1024)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = 0;
            long totalBytes = 0;
            while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                targetStream.Write(buffer, 0, bytesRead);
                totalBytes += bytesRead;
            }
            return totalBytes;
        }

        public static byte[] GetBufferWithoutZeroTail(this MemoryStream memoryStream)
        {
            memoryStream.Position = 0;

            var buffer = new byte[memoryStream.Length];
            memoryStream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        public static long GetNextBlockIndex(this Stream inputStream, byte[] blockHeader, int readBlockBufferSize = 1024)
        {
            while (inputStream.Position < inputStream.Length)
            {
                long startPosition = inputStream.Position;

                byte[] buffer = new byte[readBlockBufferSize];
                if (inputStream.Read(buffer, 0, buffer.Length) == 0)
                    break;

                var arrayIndexes = GetSubArrayIndexes(buffer, blockHeader);
                if (arrayIndexes.Length > 0)
                {
                    inputStream.Position = arrayIndexes.Length == 1 ? startPosition + readBlockBufferSize : startPosition + arrayIndexes[1];
                    return startPosition + arrayIndexes[0];
                }

                inputStream.Position -= blockHeader.Length;
            }

            return -1;
        }

        public static long[] GetBlockIndexes(this Stream inputStream, byte[] blockHeader, int readBlockBufferSize = 1024)
        {
            var blockIndexes = new List<long>();

            while (true)
            {
                long startPosition = inputStream.Position;

                byte[] buffer = new byte[readBlockBufferSize];
                if (inputStream.Read(buffer, 0, buffer.Length) == 0)
                    break;
                
                var arrayIndexes = GetSubArrayIndexes(buffer, blockHeader);
                blockIndexes.AddRange(arrayIndexes.Select(x => x + startPosition));

                if (inputStream.Position >= inputStream.Length)
                {
                    break;
                }

                inputStream.Position -= blockHeader.Length;
            }

            return blockIndexes.Distinct().ToArray();
        }

        public static long[] GetSubArrayIndexes(byte[] array, byte[] subArray)
        {
            var indexes = new List<long>(); 

            for (int i = 0; i < array.Length; i++)
            {
                if (ContainsSubArray(array, i, subArray))
                {
                    indexes.Add(i);
                }
            }

            return indexes.ToArray();
        }

        public static int GetSubArrayIndex(byte[] array, byte[] subArray)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (ContainsSubArray(array, i, subArray))
                {
                    return i;
                }
            }

            return -1;
        }

        public static bool ContainsSubArray(byte[] array, int startIndex, byte[] subArray)
        {
            if (startIndex < 0 || startIndex > array.Length - subArray.Length)
            {
                return false;
            }

            for (int i = 0; i < subArray.Length; i++)
            {
                if (array[startIndex + i] != subArray[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}