using System;
using System.Collections.Generic;

namespace GZipCompressor
{
    /// <summary>
    /// ��������� ������� ���������� ��������
    /// </summary>
    public class CompletedEventArgs : EventArgs
    {
        public static CompletedEventArgs Success(long inputFileSize, long outputFileSize)
        {
            return new CompletedEventArgs(CompletionStatus.Successed, inputFileSize, outputFileSize, null);
        }

        public static CompletedEventArgs Cancell()
        {
            return new CompletedEventArgs(CompletionStatus.Cancelled, 0 , 0, null);
        }

        public static CompletedEventArgs Fault(List<Exception> exceptions)
        {
            return new CompletedEventArgs(CompletionStatus.Faulted, 0, 0, exceptions);
        }

        private CompletedEventArgs(
            CompletionStatus status,
            long inputFileSize,
            long outputFileSize,
            IEnumerable<Exception> exceptions)
        {
            Status = status;
            InputFileSize = inputFileSize;
            OutputFileSize = outputFileSize;
            Exceptions = exceptions == null ? new List<Exception>() : new List<Exception>(exceptions);
        }

        /// <summary>
        /// ������ ��������
        /// </summary>
        public CompletionStatus Status { get; private set; }

        /// <summary>
        /// ������ ��������� �����
        /// </summary>
        public long InputFileSize { get; private set; }

        /// <summary>
        /// ������ ���������������� �����
        /// </summary>
        public long OutputFileSize { get; private set; }

        /// <summary>
        /// ������ �� ����� ���������� ��������
        /// </summary>
        public List<Exception> Exceptions { get; private set; }
    }
}