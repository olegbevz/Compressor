namespace GZipCompressor
{
    /// <summary>
    /// Статус завершения операции
    /// </summary>
    public enum CompletionStatus
    {
        /// <summary>
        /// Операция завершилась успешно
        /// </summary>
        Successed,

        /// <summary>
        /// Операция была отменена
        /// </summary>
        Cancelled,

        /// <summary>
        /// Операция завершилась с ошибками
        /// </summary>
        Faulted
    }
}