using System;
using System.IO;
using System.Text;

namespace SymbolSource.Processor.Legacy
{
    public class FileCompressor : IFileCompressor
    {
        private readonly ConsoleRunner runner;

        public FileCompressor()
        {
           runner = new ConsoleRunner(null, "makecab.exe");
        }

        public void Dispose()
        {
            runner.Dispose();
        }

        public void Compress(string fileName, Stream source, Stream destination)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);

            var sourcePath = Path.Combine(tempDirectory, fileName);
            var destinationPath = Path.Combine(tempDirectory, fileName + "_");

            try
            {
                using (var tempStream = File.OpenWrite(sourcePath))
                    source.CopyTo(tempStream);

                Compress(sourcePath, destinationPath);

                using (var tempStream = File.OpenRead(destinationPath))
                    tempStream.CopyTo(destination);
            }
            finally
            {
                File.Delete(sourcePath);
                File.Delete(destinationPath);
                Directory.Delete(tempDirectory);
            }
        }

        private void Compress(string source, string destination)
        {
            runner.Run(
                output =>
                {
                    // ReSharper disable once ConvertToLambdaExpression
                    return null;
                },
                error =>
                {
                    throw new Exception(error);
                },
                new[]
                {
                    source,
                    destination
                },
                true,
                null,
                Encoding.UTF8
                );
        }
    }
}
