using System.Collections.Generic;
using System.IO;

namespace GZipCompressor
{
    /// <summary>
    /// Набор вспомогательных методов для работы с потоками
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        /// Размер блока для чтения потока байтов
        /// </summary>
        private const int DEFAULT_READ_BLOCK_SIZE = 1024;

        /// <summary>
        /// Получить массив байтов из потока в памяти.
        /// В отличие от метода MemoryStream::GetBuffer метод при больших объемах данных 
        /// возвращает массив байтов без нулевых байтов в конце.
        /// </summary>
        /// <param name="memoryStream">Поток в памяти</param>
        /// <returns></returns>
        public static byte[] GetBufferWithoutZeroTail(this MemoryStream memoryStream)
        {
            memoryStream.Position = 0;

            var buffer = new byte[memoryStream.Length];
            memoryStream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        /// <summary>
        /// Поток начинается с указанного массива байтов.
        /// </summary>
        /// <param name="inputStream">Поток байтов</param>
        /// <param name="buffer">Массив байтов</param>
        /// <returns>Поток байтов начи нается указанного массива байтов</returns>
        public static bool StartsWith(this Stream inputStream, byte[] buffer)
        {
            byte[] streamBuffer = new byte[buffer.Length];
            if (inputStream.Position > 0)
                inputStream.Seek(0, SeekOrigin.Begin);

            if (inputStream.Read(streamBuffer, 0, streamBuffer.Length) > 0)
            {
                inputStream.Seek(0, SeekOrigin.Begin);
                return CompareArrays(streamBuffer, 0, buffer);
            }

            return false;
        }

        /// <summary>
        /// Метод возвращает первое вхождение указанного массива байтов
        /// </summary>
        /// <param name="inputStream">Поток байтов</param>
        /// <param name="blockHeader">Массив байтов</param>
        /// <param name="readBlockSize">Размер блока для чтения потока байтов</param>
        /// <returns>Первое вхождение указанного массива байтов</returns>
        public static long GetBufferIndex(this Stream inputStream, byte[] blockHeader, int readBlockSize = DEFAULT_READ_BLOCK_SIZE)
        {
            while (inputStream.Position < inputStream.Length)
            {
                long startPosition = inputStream.Position;

                byte[] buffer = new byte[readBlockSize];
                if (inputStream.Read(buffer, 0, buffer.Length) == 0)
                    break;

                var arrayIndexes = GetSubArrayIndexes(buffer, blockHeader);
                if (arrayIndexes.Length > 0)
                {
                    inputStream.Position = arrayIndexes.Length == 1 ? startPosition + readBlockSize : startPosition + arrayIndexes[1];
                    return startPosition + arrayIndexes[0];
                }

                if (inputStream.Position == inputStream.Length)
                    break;

                inputStream.Position -= blockHeader.Length;
            }

            return -1;
        }

        private static long[] GetSubArrayIndexes(byte[] array, byte[] subArray)
        {
            var indexes = new List<long>(); 

            for (int i = 0; i < array.Length; i++)
            {
                if (CompareArrays(array, i, subArray))
                {
                    indexes.Add(i);
                }
            }

            return indexes.ToArray();
        }

        private static bool CompareArrays(byte[] array, int startIndex, byte[] arrayToCompare)
        {
            if (startIndex < 0 || startIndex > array.Length - arrayToCompare.Length)
            {
                return false;
            }

            for (int i = 0; i < arrayToCompare.Length; i++)
            {
                if (array[startIndex + i] != arrayToCompare[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}