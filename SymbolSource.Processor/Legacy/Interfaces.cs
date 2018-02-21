using System.Collections.Generic;
using System.IO;

namespace SymbolSource.Processor.Legacy
{
    public interface IBinaryStoreManager
    {
        string ReadPdbHash(string binaryFilePath);
        string ReadBinaryHash(string binaryFilePath);
        string ReadPdbHash(Stream stream);
        string ReadBinaryHash(Stream stream);
    }


    public interface IFileCompressor
    {
        void Compress(string fileName, Stream source, Stream destination);
    }

    public interface ISymbolStoreManager
    {
        string ReadHash(Stream stream);
        void WriteHash(string pdbFilePath, string hash);
    }

    public interface ISourceExtractor
    {
        IList<string> ReadSources(Stream pdbStream);
    }

    public interface ISourceStoreManager
    {
        string ReadHash(Stream stream);
    }
}