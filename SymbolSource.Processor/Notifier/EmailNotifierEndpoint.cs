using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SymbolSource.Contract;

namespace SymbolSource.Processor.Notifier
{
    public class EmailNotifierEndpoint : INotifierEndpoint
    {
        public Task Damaged(UserInfo userInfo, PackageName packageName)
        {
            return Task.Delay(0);
        }

        public Task Starting(UserInfo userInfo, PackageName packageName)
        {
            return Task.Delay(0);
        }

        public Task Indexed(UserInfo userInfo, PackageName packageName)
        {
            return Task.Delay(0);
        }

        public Task Deleted(UserInfo userInfo, PackageName packageName)
        {
            return Task.Delay(0);
        }

        public Task PartiallyIndexed(UserInfo userInfo, PackageName packageName)
        {
            return Task.Delay(0);
        }

        public Task PartiallyDeleted(UserInfo userInfo, PackageName packageName)
        {
            return Task.Delay(0);
        }
    }
}
