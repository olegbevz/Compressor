using System;

namespace Compressor
{
    public interface ICompressionUnit
    {
        event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        event EventHandler<CompletedEventArgs> Completed;
        void Execute(string inputPath, string outputPath);
        void Cancel();
        
    }
}