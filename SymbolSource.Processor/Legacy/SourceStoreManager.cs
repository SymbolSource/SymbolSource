using System.IO;
using System.Security.Cryptography;
using System.Text;
using SymbolSource.Contract;

namespace SymbolSource.Processor.Legacy
{
    public class SourceStoreManager : ISourceStoreManager
    {       
        public string ReadHash(Stream stream)
        {
            using (var algorithm = new SHA256CryptoServiceProvider())
            {
                var hash = algorithm.ComputeHash(stream);
                return hash.ToInternetBase64();
            }
        }
    }
}