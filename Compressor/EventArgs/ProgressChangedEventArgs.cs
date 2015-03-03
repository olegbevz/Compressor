using System;

namespace GZipCompressor
{
    /// <summary>
    /// Аргументы события изменения процента выполнения операции
    /// </summary>
    public class ProgressChangedEventArgs : EventArgs
    {
        public ProgressChangedEventArgs(double progressPercentage)
        {
            ProgressPercentage = progressPercentage;
        }

        /// <summary>
        /// Процент выполнения операции
        /// </summary>
        public double ProgressPercentage { get; private set; }
    }
}