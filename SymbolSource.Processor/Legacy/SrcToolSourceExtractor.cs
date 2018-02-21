using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SymbolSource.Processor.Legacy
{
    public class SrcToolSourceExtractor : ISourceExtractor, IDisposable
    {
        private readonly ConsoleRunner runner;

        public SrcToolSourceExtractor()
        {
            runner = new ConsoleRunner(null, "srctool.exe", EmbeddedProgramComponent.GetAll(GetType(), "srctool.exe"));
        }

        public void Dispose()
        {
            runner.Dispose();
        }

        public IList<string> ReadSources(Stream pdbStream)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".pdb");

            try
            {
                using (var tempStream = File.OpenWrite(tempPath))
                    pdbStream.CopyTo(tempStream);

                return ReadSources(tempPath);
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        private IList<string> ReadSources(string path)
        {
            var builder = new StringBuilder();

            runner.Run(
                output =>
                {
                    builder.AppendLine(output);
                    return null;
                },
                error =>
                {
                    throw new Exception(error);
                },
                new[]
                {
                    "-r",
                    string.Format("\"{0}\"", path),
                },
                false,
                null,
                Encoding.UTF8);

            return builder.ToString()
                .Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToArray();
        }
    }
}
