using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Formatter;
using System.Web.Http.OData.Formatter.Deserialization;
using System.Web.Http.OData.Formatter.Serialization;
using System.Web.Http.OData.Query;
using System.Web.Http.Routing;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using NuGet;
using SymbolSource.Contract;
using SymbolSource.Contract.Scheduler;
using SymbolSource.Contract.Security;
using SymbolSource.Contract.Storage;
using SymbolSource.Contract.Support;
using SymbolSource.Server.Authentication;
using SymbolSource.Server.Helper;
using PackageName = SymbolSource.Contract.PackageName;

namespace SymbolSource.Server
{
    public class MethodRouteAttribute : RouteFactoryAttribute
    {
        public MethodRouteAttribute(string template)
            : base(template)
        {
        }

        public override IDictionary<string, object> Constraints
        {
            get
            {
                var constraints = new HttpRouteValueDictionary();
                constraints["httpMethod"] = new HttpMethodConstraint(HttpMethod.Put);
                return constraints;
            }
        }
    }

    public partial class DataController
    {
        public const string DownloadDefaultRouteName = "DownloadDefault";
        public const string DownloadNamedRouteName = "DownloadNamed";

        [AuthenticatedArea(AuthenticatedArea.Pushing, "feedName")]
        [MethodRouteAttribute("api/v2/package")]
        [MethodRouteAttribute("{feedName}")]
        [HttpPut]
        public async Task<IHttpActionResult> Put()
        {
            var principal = (FeedPrincipal)User;
            Trace.TraceInformation("{0} - {1} byte(s)", principal, Request.Content.Headers.ContentLength);

            if (!principal.Identity.IsAuthenticated)
                throw new InvalidOperationException("Identity is required to put packages");

            var provider = new StorageMultipartStreamProvider(storage, scheduler, principal);
            await Request.Content.ReadAsMultipartAsync(provider);

            var forbidden = 0;
            var corrupted = 0;

            foreach (var packageInfo in provider.Packages)
            {
                packageInfo.Value.Position = 0;
                PackageName packageName = null;

                try
                {
                    var package = new ZipPackage(packageInfo.Value);
                    packageName = new PackageName(package.Id, package.Version.ToString());
                }
                catch (Exception e)
                {
                    Trace.TraceInformation("{0} - corrupted package", principal);
                    support.TrackException(e, null);
                    corrupted++;
                }

                if (packageName == null)
                {
                    await packageInfo.Key.Delete();
                    continue;
                }

                if (!security.Authorize(principal, new[] { packageName }))
                {
                    Trace.TraceInformation("{0} - not authorizated to push {1}", principal, packageName);
                    forbidden++;
                    await packageInfo.Key.Delete();
                    continue;
                }

                Trace.TraceInformation("Signaling package {0} ({1})", packageInfo.Key.Name, packageName);
                await scheduler.Signal(principal, PackageState.New, packageInfo.Key.Name);
            }

            if (corrupted > 0)
                return BadRequest();

            if (forbidden > 0)
                return Forbidden();

            return Ok();
        }

        [AuthenticatedArea(AuthenticatedArea.Querying, "feedName")]
        [Route("api/v2/package/{packageId}/{packageVersion}", Name = DownloadDefaultRouteName)]
        [Route("{feedName}/{packageId}/{packageVersion}", Name = DownloadNamedRouteName)]
        [HttpGet]
        public async Task<IHttpActionResult> Download(string packageId, string packageVersion)
        {
            var principal = (FeedPrincipal)User;
            var packageName = new PackageName(packageId, packageVersion);
            Trace.TraceInformation("{0} - downloading package {1}", principal, packageName);

            if (!security.Authorize(principal, new[] { packageName }))
            {
                Trace.TraceInformation("{0} - not authorizated to download package {1}", principal, packageName);
                return Unauthorized();
            }

            if (principal.PackageState == PackageState.New)
                return BadRequest("Downloading new packages is not supported. Wait until they are at least queued.");

            var package = storage.GetFeed(principal.FeedName).GetPackage(principal.Identity.Name, principal.PackageState, packageName);
            return await Storage(package);
        }

        [AuthenticatedArea(AuthenticatedArea.Deleting, "feedName")]
        [Route("api/v2/package/{packageId}/{packageVersion}")]
        [Route("{feedName}/{packageId}/{packageVersion}")]
        [HttpDelete]
        public async Task<IHttpActionResult> Delete(string packageId, string packageVersion)
        {
            var principal = (FeedPrincipal)User;
            var packageName = new PackageName(packageId, packageVersion);
            Trace.TraceInformation("{0} - deleting package {1}", principal, packageName);

            if (!security.Authorize(principal, new[] { packageName }))
            {
                Trace.TraceInformation("{0} - not authorizated to delete package {1}", principal, packageName);
                return Forbidden();
            }

            if (!principal.Identity.IsAuthenticated)
                throw new InvalidOperationException("Identity is required to delete packages");

            var package = storage.GetFeed(principal.FeedName).GetPackage(null, principal.PackageState, packageName);

            //if (principal.AuthenticatedArea == AuthenticatedArea.DeletingOwn && package.UserName != identity.Name)
            //    return NotFound();

            if (principal.PackageState == PackageState.Succeded || principal.PackageState == PackageState.Partial)
            {
                if (await package.Move(PackageState.DeletingQueued, packageName) == null)
                    return NotFound();

                await scheduler.Signal(principal, PackageState.DeletingQueued, packageName);
            }
            else
            {
                if (!await package.Delete())
                    return NotFound();
            }

            return Ok();
        }
    }

    public class CustomODataFormattingAttribute : ODataFormattingAttribute
    {
        public override IList<ODataMediaTypeFormatter> CreateODataFormatters()
        {
            var formatters = ODataMediaTypeFormatters.Create(
                new MyODataSerializerProvider(), new DefaultODataDeserializerProvider());

            return formatters;
        }
    }

    public class MyODataSerializerProvider : DefaultODataSerializerProvider
    {
        public override ODataEdmTypeSerializer GetEdmTypeSerializer(IEdmTypeReference edmType)
        {
            if (edmType.IsEntity())
                return new CustomODataEntityTypeSerializer(this);

            return base.GetEdmTypeSerializer(edmType);
        }
    }

    public class CustomODataEntityTypeSerializer : ODataEntityTypeSerializer
    {
        public CustomODataEntityTypeSerializer(ODataSerializerProvider serializerProvider)
            : base(serializerProvider)
        {
        }

        public override ODataEntry CreateEntry(SelectExpandNode selectExpandNode, EntityInstanceContext entityInstanceContext)
        {
            var entry = base.CreateEntry(selectExpandNode, entityInstanceContext);

            entry.MediaResource = new ODataStreamReferenceValue
            {
                ContentType = System.Net.Mime.MediaTypeNames.Application.Octet,
                ReadLink = new Uri(GetLink(entityInstanceContext))
            };

            return entry;
        }

        private static string GetLink(EntityInstanceContext entityInstanceContext)
        {
            var package = (V2FeedPackage)entityInstanceContext.EntityInstance;
            var routeData = entityInstanceContext.Request.GetRouteData();

            //TODO: wybrać Named lub Default
            var downloadRouteName = routeData.Values.ContainsKey("feedName")
                ? DataController.DownloadNamedRouteName
                : DataController.DownloadDefaultRouteName;

            return entityInstanceContext.Url.Link(downloadRouteName, new
            {
                packageId = package.Id,
                packageVersion = package.Version
            });
        }
    }

    [CustomODataFormatting]
    [ODataRouting]
    public class PackagesController : FeedApiController
    {
        public PackagesController(
            ISecurityService security,
            IStorageService storage,
            ISchedulerService scheduler,
            ISupportService support)
            : base(security, storage, scheduler, support)
        {
        }

        [HttpGet]
        public async Task<PageResult<V2FeedPackage>> Search(string searchTerm, ODataQueryOptions options)
        {
            searchTerm = searchTerm.Trim('\'');
            var principal = (FeedPrincipal)User;
            Trace.TraceInformation("{0} - {1} ({2}-{3})", principal, searchTerm, options.Skip, options.Top);

            if (principal.AuthenticatedArea == AuthenticatedArea.Querying)
                principal.AuthenticatedArea = principal.Identity.IsAuthenticated
                    ? AuthenticatedArea.QueryingOwn
                    : AuthenticatedArea.QueryingAll;

            if (!security.Authorize(principal, null))
            {
                Trace.TraceInformation("{0} - not authorizated", principal);
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }

            var entities = (await Query(principal, searchTerm, options.Skip, options.Top))
                .Select(p => new V2FeedPackage
                {
                    Id = p.Id,
                    Title = p.Id,
                    Created = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    LastEdited = DateTime.Now,
                    Published = DateTime.Now,
                    Authors = "",
                    Copyright = "",
                    Dependencies = "",
                    Description = "",
                    GalleryDetailsUrl = "",
                    IconUrl = "",
                    Listed = true,
                    IsLatestVersion = false,
                    IsAbsoluteLatestVersion = false,
                    Language = "",
                    LicenseNames = "",
                    LicenseReportUrl = "",
                    LicenseUrl = "",
                    NormalizedVersion = "",
                    PackageHash = "",
                    PackageHashAlgorithm = "",
                    ProjectUrl = "",
                    ReleaseNotes = "",
                    ReportAbuseUrl = "",
                    Summary = "",
                    Tags = "",
                    Version = p.Version,
                })
                .ToList();

            if (options.Skip == null || options.Top == null)
                return new PageResult<V2FeedPackage>(entities, null, entities.Count);

            var count = entities.Count < options.Top.Value
                ? options.Skip.Value + entities.Count
                : options.Skip.Value + 2 * options.Top.Value;

            return new PageResult<V2FeedPackage>(entities, null, count);
        }

        public Task<PageResult<V2FeedPackage>> Get(ODataQueryOptions options)
        {
            return Search("", options);
        }

        private Task<IEnumerable<PackageName>> Query(FeedPrincipal principal, string packageNamePrefix, SkipQueryOption skip, TopQueryOption top)
        {
            var feed = storage.GetFeed(principal.FeedName);

            switch (principal.AuthenticatedArea)
            {
                case AuthenticatedArea.QueryingOwn:
                    return skip != null && top != null
                        ? feed.QueryPackages(principal.Identity.Name, principal.PackageState, packageNamePrefix, skip.Value, top.Value)
                        : feed.QueryPackages(principal.Identity.Name, principal.PackageState);
                case AuthenticatedArea.QueryingAll:
                    return skip != null && top != null
                        ? feed.QueryPackages(principal.PackageState, packageNamePrefix, skip.Value, top.Value)
                        : feed.QueryPackages(principal.PackageState);
                default:
                    throw new ArgumentOutOfRangeException("area", principal.AuthenticatedArea, null);
            }
        }
    }

    public static class EnumExtensions
    {
        public static T Parse<T>(string value) where T : struct
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static bool TryParse<T>(string value, ref T result) where T : struct
        {
            T newResult;
            var newParsed = Enum.TryParse(value, true, out newResult);

            if (newParsed)
                result = newResult;

            return newParsed;
        }
    }
}