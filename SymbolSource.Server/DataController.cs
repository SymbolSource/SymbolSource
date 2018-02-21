using System.ComponentModel;
using System.Web.Http;
using SymbolSource.Contract.Scheduler;
using SymbolSource.Contract.Security;
using SymbolSource.Contract.Storage;
using SymbolSource.Contract.Support;
using SymbolSource.Support;

namespace SymbolSource.Server
{
    public partial class DataController : FeedApiController
    {
        public DataController(
            ISecurityService security,
            IStorageService storage, 
            ISchedulerService scheduler, 
            ISupportService support) 
            : base(security, storage, scheduler, support)
        {
        }
    }
}