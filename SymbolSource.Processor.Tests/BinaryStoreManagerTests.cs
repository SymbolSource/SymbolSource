using System.IO;
using SymbolSource.Processor.Legacy;
using Xunit;

namespace SymbolSource.Processor.Tests
{
    public class BinaryStoreManagerTests
    {
        IBinaryStoreManager manager = new BinaryStoreManager();

        private Stream GetStream(string name)
        {
            var type = GetType();
            var fullName = string.Format("{0}.{1}.{2}.dll", type.Namespace, type.Name, name);
            return type.Assembly.GetManifestResourceStream(fullName);
        }

        [Fact]
        public void NETBinaryWithPdb_DllHash()
        {
            using (var stream = GetStream("NETBinaryWithPdb"))
            {
                string hash = manager.ReadBinaryHash(stream);
                Assert.Equal("4B4B41A11A000", hash);
            }

        }

        [Fact]
        public void NETBinaryWithPdb_PdbHash()
        {
            using (var stream = GetStream("NETBinaryWithPdb"))
            {
                string hash = manager.ReadPdbHash(stream);
                Assert.Equal("74D5496D80E64ED5A87D020228ACB5711", hash);
            }
            
        }

        [Fact]
        public void NETBinaryWithOutPdb_DllHash()
        {
            using (var stream = GetStream("NETBinaryWithOutPdb"))
            {
                string hash = manager.ReadBinaryHash(stream);
                Assert.Equal("4BFB3C1B34000", hash);
            }

        }

        [Fact]
        public void NETBinaryWithOutPdb_PdbHash()
        {
            using (var stream = GetStream("NETBinaryWithOutPdb"))
            {
                string hash = manager.ReadPdbHash(stream);
                Assert.Null(hash);
            }

        }

        [Fact]
        public void CPlusPlusBinary_DllHash()
        {
            using (var stream = GetStream("CPlusPlusBinary"))
            {
                string hash = manager.ReadBinaryHash(stream);
                Assert.Equal("419E050922000", hash);
            }

        }
    }
}
