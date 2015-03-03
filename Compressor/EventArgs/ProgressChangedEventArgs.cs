using System;

namespace GZipCompressor
{
    /// <summary>
    /// ��������� ������� ��������� �������� ���������� ��������
    /// </summary>
    public class ProgressChangedEventArgs : EventArgs
    {
        public ProgressChangedEventArgs(double progressPercentage)
        {
            ProgressPercentage = progressPercentage;
        }

        /// <summary>
        /// ������� ���������� ��������
        /// </summary>
        public double ProgressPercentage { get; private set; }
    }
}