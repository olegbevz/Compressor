using System;

namespace GZipCompressor
{
    /// <summary>
    /// ��������� ��� ������ ��������/���������� �����
    /// </summary>
    public interface ICompressionUnit
    {
        /// <summary>
        /// ������� ��������� �������� ���������� ��������
        /// </summary>
        event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// ������� ���������� ��������
        /// </summary>
        event EventHandler<CompletedEventArgs> Completed;

        /// <summary>
        /// ������ �������� � ����������� ������
        /// </summary>
        /// <param name="inputPath">��� �������� �����</param>
        /// <param name="outputPath">��� ��������� �����</param>
        void Execute(string inputPath, string outputPath);

        /// <summary>
        /// ������ ��������
        /// </summary>
        void Cancel();
    }
}