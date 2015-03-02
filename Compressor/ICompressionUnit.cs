using System;

namespace GZipCompressor
{
    /// <summary>
    /// Интерфейс для модуля упаковки/распаковки файла
    /// </summary>
    public interface ICompressionUnit
    {
        /// <summary>
        /// Событие изменения процента выполнения операции
        /// </summary>
        event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Событие завершения операции
        /// </summary>
        event EventHandler<CompletedEventArgs> Completed;

        /// <summary>
        /// Запуск операции в асинхронном режиме
        /// </summary>
        /// <param name="inputPath">Имя входного файла</param>
        /// <param name="outputPath">Имя выходного файла</param>
        void Execute(string inputPath, string outputPath);

        /// <summary>
        /// Отмена операции
        /// </summary>
        void Cancel();
    }
}