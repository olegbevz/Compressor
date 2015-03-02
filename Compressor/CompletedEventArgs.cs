using System;
using System.Collections.Generic;

namespace Compressor
{
    public class CompletedEventArgs : EventArgs
    {
        public static CompletedEventArgs Success()
        {
            return new CompletedEventArgs(CompletionStatus.Successed, null);
        }

        public static CompletedEventArgs Cancell()
        {
            return new CompletedEventArgs(CompletionStatus.Cancelled, null);
        }

        public static CompletedEventArgs Fault(List<Exception> exceptions)
        {
            return new CompletedEventArgs(CompletionStatus.Faulted, exceptions);
        }

        private CompletedEventArgs(
            CompletionStatus status,
            IEnumerable<Exception> exceptions)
        {
            Status = status;

            if (exceptions != null)
                Exceptions = new List<Exception>(exceptions);
        }

        public CompletionStatus Status { get; private set; }

        public List<Exception> Exceptions { get; private set; }
    }

    public enum CompletionStatus
    {
        Successed,
        Cancelled,
        Faulted
    }
}