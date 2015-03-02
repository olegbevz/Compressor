using System;

namespace GZipCompressor
{
    public class ProgressChangedEventArgs : EventArgs
    {
        public ProgressChangedEventArgs(double progressPercentage)
        {
            ProgressPercentage = progressPercentage;
        }

        public double ProgressPercentage { get; private set; }
    }
}