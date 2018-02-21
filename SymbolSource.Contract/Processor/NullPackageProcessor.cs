using System;
using System.Threading.Tasks;

namespace SymbolSource.Contract.Processor
{
    public class NullPackageProcessor : IPackageProcessor
    {
        public Task Process(PackageMessage message)
        {
            throw new NotImplementedException();
        }
    }
}