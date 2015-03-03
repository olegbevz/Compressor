using System;
using System.Collections.Generic;

namespace GZipCompressor
{
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

            if (exceptions != null)
                Exceptions = new List<Exception>(exceptions);
        }

        public CompletionStatus Status { get; private set; }

        public long InputFileSize { get; private set; }

        public long OutputFileSize { get; private set; }

        public List<Exception> Exceptions { get; private set; }
    }
}