namespace GZipCompressor
{
    /// <summary>
    /// ������ ���������� ��������
    /// </summary>
    public enum CompletionStatus
    {
        /// <summary>
        /// �������� ����������� �������
        /// </summary>
        Successed,

        /// <summary>
        /// �������� ���� ��������
        /// </summary>
        Cancelled,

        /// <summary>
        /// �������� ����������� � ��������
        /// </summary>
        Faulted
    }
}