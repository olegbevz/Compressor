using System.Collections.Generic;
using System.IO;

namespace GZipCompressor
{
    /// <summary>
    /// ����� ��������������� ������� ��� ������ � ��������
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        /// ������ ����� ��� ������ ������ ������
        /// </summary>
        private const int DEFAULT_READ_BLOCK_SIZE = 1024;

        /// <summary>
        /// �������� ������ ������ �� ������ � ������.
        /// � ������� �� ������ MemoryStream::GetBuffer ����� ��� ������� ������� ������ 
        /// ���������� ������ ������ ��� ������� ������ � �����.
        /// </summary>
        /// <param name="memoryStream">����� � ������</param>
        /// <returns></returns>
        public static byte[] GetBufferWithoutZeroTail(this MemoryStream memoryStream)
        {
            memoryStream.Position = 0;

            var buffer = new byte[memoryStream.Length];
            memoryStream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        /// <summary>
        /// ����� ���������� � ���������� ������� ������.
        /// </summary>
        /// <param name="inputStream">����� ������</param>
        /// <param name="buffer">������ ������</param>
        /// <returns>����� ������ ���� ������ ���������� ������� ������</returns>
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
        /// ����� ���������� ������ ��������� ���������� ������� ������
        /// </summary>
        /// <param name="inputStream">����� ������</param>
        /// <param name="blockHeader">������ ������</param>
        /// <param name="readBlockSize">������ ����� ��� ������ ������ ������</param>
        /// <returns>������ ��������� ���������� ������� ������</returns>
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