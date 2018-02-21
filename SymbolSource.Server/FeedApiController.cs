using System.Net;
using System.Security.Principal;
using System.Web.Http;
using System.Web.Http.Results;
using SymbolSource.Contract;
using SymbolSource.Contract.Scheduler;
using SymbolSource.Contract.Security;
using SymbolSource.Contract.Storage;
using SymbolSource.Contract.Support;
using SymbolSource.Support;

namespace SymbolSource.Server
{
    public class FeedApiController : ApiController
    {
        protected readonly ISecurityService security;
        protected readonly IStorageService storage;
        protected readonly ISchedulerService scheduler;
        protected readonly ISupportService support;

        public FeedApiController(
           ISecurityService security,
           IStorageService storage,
           ISchedulerService scheduler,
           ISupportService support)
        {
            this.security = security;
            this.storage = storage;
            this.scheduler = scheduler;
            this.support = support;
        }

        protected IHttpActionResult Forbidden()
        {
            return new StatusCodeResult(HttpStatusCode.Forbidden, this);
        }
    }
}