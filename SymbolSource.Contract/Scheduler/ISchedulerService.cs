using System.Threading;
using System.Threading.Tasks;

namespace SymbolSource.Contract.Scheduler
{
    public interface ISchedulerService
    {
        Task Signal(PackageMessage message);
        void ListenAndProcess(CancellationToken cancellationToken);
    }
}