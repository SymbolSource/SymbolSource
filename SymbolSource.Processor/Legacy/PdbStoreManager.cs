using System;
using System.IO;
using System.Text;

namespace SymbolSource.Processor.Legacy
{
    public interface IPdbStoreConfig
    {
        string SrcSrvPath { get; set; }
    }

    public interface IPdbStoreManager
    {
        PdbSrcSrvSection ReadSrcSrv(Stream input);
        void WriteSrcSrv(Stream input, Stream output, PdbSrcSrvSection section);
    }

    public class PdbStoreManager : IPdbStoreManager, IDisposable
    {
        private readonly ConsoleRunner runner;

        public PdbStoreManager()
        {
           runner = new ConsoleRunner(null, "pdbstr.exe", EmbeddedProgramComponent.GetAll(GetType(), "pdbstr.exe"));
        }

        public void Dispose()
        {
            runner.Dispose();
        }

        public PdbSrcSrvSection ReadSrcSrv(Stream input)
        {
            var tempPath = Path.GetTempFileName();

            try
            {
                using (var tempStream = File.OpenWrite(tempPath))
                    input.CopyTo(tempStream);

                return ReadSrcSrv(tempPath);
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        private PdbSrcSrvSection ReadSrcSrv(string path) 
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
                    string.Format("-p:\"{0}\"", path),
                    "-s:srcsrv"
                },
                false,
                null,
                Encoding.UTF8);

            return PdbSrcSrvSection.Parse(builder.ToString());
        }

        public void WriteSrcSrv(Stream input, Stream output, PdbSrcSrvSection section)
        {
            var tempPath = Path.Combine(runner.WorkingPath, Path.GetRandomFileName());

            try
            {
                using (var tempStream = File.OpenWrite(tempPath))
                    input.CopyTo(tempStream);

                WriteSrcSrv(tempPath, section);

                using (var tempStream = File.OpenRead(tempPath))
                    tempStream.CopyTo(output);
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        private void WriteSrcSrv(string path, PdbSrcSrvSection section)
        {
            var tempPath = Path.Combine(runner.WorkingPath, Path.GetRandomFileName());

            try
            {
                File.WriteAllText(tempPath, section.ToString());

                runner.Run(
                    output =>
                    {
                        throw new Exception(output);
                    },
                    error =>
                    {
                        throw new Exception(error);
                    },
                    new[]
                    {
                        "-w",
                        string.Format("-p:{0}", path),
                        string.Format("-i:{0}", tempPath),
                        "-s:srcsrv"
                    },
                    false,
                    null,
                    Encoding.UTF8);
            }
            finally
            {
                File.Delete(tempPath);
            }          
        }
    }
}