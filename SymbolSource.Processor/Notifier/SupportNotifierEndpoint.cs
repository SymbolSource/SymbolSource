using System.Threading.Tasks;
using SymbolSource.Contract;
using SymbolSource.Contract.Support;

namespace SymbolSource.Processor.Notifier
{
    public class SupportNotifierEndpoint : INotifierEndpoint
    {
        private readonly ISupportService support;

        public SupportNotifierEndpoint(ISupportService support)
        {
            this.support = support;
        }

        public Task Starting(UserInfo userInfo, PackageName packageName)
        {
            support.TrackEvent(userInfo, "package-submitted", new { packageName });
            return Task.Delay(0);
        }

        public Task Damaged(UserInfo userInfo, PackageName packageName)
        {
            support.TrackEvent(userInfo, "package-damaged", new { packageName });
            return Task.Delay(0);
        }

        public Task Indexed(UserInfo userInfo, PackageName packageName)
        {
            support.TrackEvent(userInfo, "package-succeded", new { packageName });
            return Task.Delay(0);
        }

        public Task Deleted(UserInfo userInfo, PackageName packageName)
        {
            support.TrackEvent(userInfo, "package-deleted", new { packageName });
            return Task.Delay(0);
        }

        public Task PartiallyIndexed(UserInfo userInfo, PackageName packageName)
        {
            support.TrackEvent(userInfo, "package-partially-indexed", new { packageName });
            return Task.Delay(0);
        }

        public Task PartiallyDeleted(UserInfo userInfo, PackageName packageName)
        {
            support.TrackEvent(userInfo, "package-partially-deleted", new { packageName });
            return Task.Delay(0);
        }
    }
}