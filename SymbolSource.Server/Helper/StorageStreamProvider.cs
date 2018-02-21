using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using SymbolSource.Contract;
using SymbolSource.Contract.Scheduler;
using SymbolSource.Contract.Security;
using SymbolSource.Contract.Storage;

namespace SymbolSource.Server.Helper
{
    internal class StorageMultipartStreamProvider : MultipartStreamProvider
    {
        private readonly IStorageService storage;
        private readonly ISchedulerService scheduler;
        private readonly FeedPrincipal principal;
        private readonly IDictionary<IPackageStorageItem, MemoryStream> packages;

        public StorageMultipartStreamProvider(
            IStorageService storage,
            ISchedulerService scheduler,
            FeedPrincipal principal)
        {
            this.storage = storage;
            this.scheduler = scheduler;
            this.principal = principal;
            packages = new Dictionary<IPackageStorageItem, MemoryStream>();
        }

        public IDictionary<IPackageStorageItem, MemoryStream> Packages
        {
            get { return packages; }
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            var guid = Guid.NewGuid().ToString();
            var packageName = new PackageName(guid, "1.0-unknown");
            Trace.TraceInformation("Receiving package {0}", packageName);
            var feed = storage.GetFeed(principal.FeedName);
            var packageItem = feed.GetPackage(principal.Identity.Name, PackageState.New, packageName);
            var memoryStream = new MemoryStream();
            packages.Add(packageItem, memoryStream);
            return new MirrorStream(packageItem.Put().Result, memoryStream);
        }
    }
}