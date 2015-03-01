using System;
using System.Collections.Generic;

namespace Compressor
{
    public class CompletedEventArgs : EventArgs
    {
        public static CompletedEventArgs Success()
        {
            return new CompletedEventArgs(true, false, false, null);
        }

        public static CompletedEventArgs Cancell()
        {
            return new CompletedEventArgs(false, true, false, null);
        }

        public static CompletedEventArgs Fault(List<Exception> exceptions)
        {
            return new CompletedEventArgs(false, false, true, exceptions);
        }

        private CompletedEventArgs(
            bool isSuccessed, 
            bool isCancelled, 
            bool isFaulted, 
            List<Exception> exceptions)
        {
            IsSuccessed = isSuccessed;
            IsCancelled = isCancelled;
            IsFaulted = isFaulted;
            Exceptions = exceptions;
        }

        public bool IsSuccessed { get; private set; }

        public bool IsCancelled { get; private set; }

        public bool IsFaulted { get; private set; }

        public List<Exception> Exceptions { get; private set; }
    }
}