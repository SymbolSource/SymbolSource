using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SymbolSource.Contract;

namespace SymbolSource.Processor.Notifier
{
    public interface INotifierService
    {
        Task Starting(UserInfo userInfo, PackageName packageName);
        Task Damaged(UserInfo userInfo, PackageName packageName);
        Task Indexed(UserInfo userInfo, PackageName packageName);
        Task Deleted(UserInfo userInfo, PackageName packageName);
        Task PartiallyIndexed(UserInfo userInfo, PackageName packageName);
        Task PartiallyDeleted(UserInfo userInfo, PackageName packageName);
    }

    public interface INotifierEndpoint : INotifierService
    {
    }

    public class NotifierService : INotifierService
    {
        readonly IEnumerable<INotifierEndpoint> endpoints;

        public NotifierService(IEnumerable<INotifierEndpoint> endpoints)
        {
            this.endpoints = endpoints;
        }

        public async Task Starting(UserInfo userInfo, PackageName packageName)
        {
            await Task.WhenAll(endpoints.Select(e => e.Starting(userInfo, packageName)));
        }

        public async Task Damaged(UserInfo userInfo, PackageName packageName)
        {
            await Task.WhenAll(endpoints.Select(e => e.Damaged(userInfo, packageName)));
        }

        public async Task Indexed(UserInfo userInfo, PackageName packageName)
        {
            await Task.WhenAll(endpoints.Select(e => e.Indexed(userInfo, packageName)));
        }

        public async Task Deleted(UserInfo userInfo, PackageName packageName)
        {
            await Task.WhenAll(endpoints.Select(e => e.Deleted(userInfo, packageName)));
        }

        public async Task PartiallyIndexed(UserInfo userInfo, PackageName packageName)
        {
            await Task.WhenAll(endpoints.Select(e => e.PartiallyIndexed(userInfo, packageName)));
        }

        public async Task PartiallyDeleted(UserInfo userInfo, PackageName packageName)
        {
            await Task.WhenAll(endpoints.Select(e => e.PartiallyDeleted(userInfo, packageName)));
        }
    }
}