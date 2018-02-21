using System.IO;
using System.Threading.Tasks;

namespace SymbolSource.Contract.Processor
{
    public interface IPackageProcessor
    {
        Task Process(PackageMessage message);
    }
}