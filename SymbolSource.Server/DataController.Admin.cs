using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using SymbolSource.Contract;
using SymbolSource.Contract.Scheduler;
using SymbolSource.Contract.Security;
using SymbolSource.Contract.Storage;
using SymbolSource.Server.Authentication;

namespace SymbolSource.Server
{
    public partial class DataController
    {
        //[AuthenticatedArea(AuthenticatedArea.Querying, "")]
        //[Route("test")]
        //[HttpGet]
        //public async Task<HttpResponseMessage> Test()
        //{
        //    var resp = new HttpResponseMessage(HttpStatusCode.OK)
        //    {
        //        Content = new StringContent(string.Join("\n", storage.QueryFeeds()), Encoding.UTF8, "text/plain")
        //    };
        //    return resp;
        //}

        [AuthenticatedArea(AuthenticatedArea.Retrying, "feedName")]
        [Route("api/v2/package/retry/all")]
        [Route("api/v2/package/retry/{packageId}/{packageVersion}")]
        [Route("{feedName}/retry/all")]
        [Route("{feedName}/retry/{packageId}/{packageVersion}")]
        [HttpPost]
        public async Task<IHttpActionResult> Retry(string packageId = null, string packageVersion = null)
        {
            var principal = (FeedPrincipal)User;
            Trace.TraceInformation("{0} - retrying", principal);

            if (principal.AuthenticatedArea == AuthenticatedArea.Retrying)
                principal.AuthenticatedArea = principal.Identity.IsAuthenticated
                    ? AuthenticatedArea.RetryingOwn
                    : AuthenticatedArea.RetryingAll;

            IEnumerable<PackageName> packageNames = null;

            if (!string.IsNullOrEmpty(packageId) && !string.IsNullOrEmpty(packageVersion))
                packageNames = new[] { new PackageName(packageId, packageVersion) };

            if (!security.Authorize(principal, packageNames))
            {
                Trace.TraceInformation("{0} - not authorizated to retry", principal);
                return Forbidden();
            }

            if (packageNames == null)
                packageNames = await QueryForRetrying(principal);

            var count = 0;

            foreach (var packageName in packageNames)
            {
                Trace.TraceInformation("{0} - retrying {1}", principal, packageName);

                switch (principal.PackageState)
                {
                    case PackageState.New:
                    case PackageState.IndexingQueued:
                    case PackageState.DeletingQueued:
                    case PackageState.Partial:
                        {
                            await scheduler.Signal(principal, principal.PackageState, packageName);
                            break;
                        }
                    case PackageState.DamagedNew:
                        {
                            if (!await MoveToState(principal, packageName, PackageState.New))
                                continue;

                            break;
                        }
                    case PackageState.Original:
                        {
                            if (!await CopyToState(principal, packageName, PackageState.IndexingQueued))
                                continue;

                            break;
                        }
                    case PackageState.Indexing:
                    case PackageState.DamagedIndexing:
                        {
                            if (!await MoveToState(principal, packageName, PackageState.IndexingQueued))
                                continue;

                            break;
                        }
                    case PackageState.Deleting:
                    case PackageState.DamagedDeleting:
                        {
                            if (!await MoveToState(principal, packageName, PackageState.DeletingQueued))
                                continue;

                            break;
                        }
                    case PackageState.Succeded:
                    case PackageState.Deleted:
                        {
                            return BadRequest(string.Format("Retrying not possible in {0} state", principal.PackageState));
                        }
                    default:
                        {
                            throw new ArgumentOutOfRangeException("packageState", principal.PackageState, null);
                        }
                }

                count++;
            }

            if (count == 0)
                return NotFound();

            return Ok(count);
        }

        private async Task<bool> MoveToState(FeedPrincipal principal, PackageName packageName, PackageState newPackageState )
        {
            var package = storage.GetFeed(principal.FeedName).GetPackage(null, principal.PackageState, packageName);
            var newPackageItem = await package.Move(newPackageState, packageName);

            if (newPackageItem == null)
                return false;

            await scheduler.Signal(principal, newPackageState, packageName);
            return true;
        }

        private async Task<bool> CopyToState(FeedPrincipal principal, PackageName packageName, PackageState newPackageState)
        {
            var package = storage.GetFeed(principal.FeedName).GetPackage(null, principal.PackageState, packageName);
            var newPackageItem = await package.Copy(newPackageState, packageName);

            if (newPackageItem == null)
                return false;

            await scheduler.Signal(principal, newPackageState, packageName);
            return true;
        }

        private Task<IEnumerable<PackageName>> QueryForRetrying(FeedPrincipal principal)
        {
            switch (principal.AuthenticatedArea)
            {
                case AuthenticatedArea.RetryingOwn:
                    return storage.GetFeed(principal.FeedName).QueryPackages(principal.Identity.Name, principal.PackageState);
                case AuthenticatedArea.RetryingAll:
                    return storage.GetFeed(principal.FeedName).QueryPackages(principal.PackageState);
                default:
                    throw new ArgumentOutOfRangeException("area", principal.AuthenticatedArea, null);
            }
        }
    }

    public static class SchedulerExtensions
    {
        public static Task Signal(this ISchedulerService scheduler, FeedPrincipal principal, PackageState packageState, PackageName packageName)
        {
            return scheduler.Signal(new PackageMessage
            {
                UserInfo = new UserInfo
                {
                    UserName = principal.Identity.Name,
                    UserHandle = principal.Identity.Handle
                },
                FeedName = principal.FeedName,
                PackageState = packageState,
                PackageName = packageName
            });
        }
    }
}