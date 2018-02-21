using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using SymbolSource.Contract.Storage.Azure;
using SymbolSource.Contract.Support;

namespace SymbolSource.Contract.Storage
{
    public class AzureStorageService : IStorageService
    {
        private const string FeedPrefix = "feed-";
        private const string NamedFeedPrefix = "named-";
        private const string DefaultFeedName = "default";

        private readonly CloudTableClient tableClient;
        private readonly CloudBlobClient blobClient;
        private readonly AzureQueryCache<PackageName> queryCache;

        public AzureStorageService(IAzureStorageConfiguration configuration, ISupportService support)
        {
            var account = CloudStorageAccount.Parse(configuration.ConnectionString);
            tableClient = account.CreateCloudTableClient();
            blobClient = account.CreateCloudBlobClient();
            queryCache = new AzureQueryCache<PackageName>(support);
        }

        public IStorageFeed GetFeed(string feedName)
        {
            var containerName = GetContainerName(feedName);
            var tableName = GetTableName(containerName);

            var table = tableClient.GetTableReference(tableName);
            var container = blobClient.GetContainerReference(containerName);

            return new AzureStorageFeed(feedName, table, container, queryCache);
        }

        private string GetTableName(string containerName)
        {
            var regex = new Regex("[^A-Za-z0-9]");
            return regex.Replace(containerName, "");
        }

        private static string GetContainerName(string feedName)
        {
            if (feedName != null)
            {
                if (feedName.ToLower() != feedName)
                    throw new ArgumentOutOfRangeException("feedName");

                feedName = NamedFeedPrefix + feedName;
            }

            if (feedName == null)
                feedName = DefaultFeedName;

            feedName = FeedPrefix + feedName;
            return feedName;
        }

        public IEnumerable<string> QueryFeeds()
        {
            return blobClient.ListContainers()
                .Select(container => container.Name)
                .Where(name => name.StartsWith(FeedPrefix))
                .Select(name => name.Substring(FeedPrefix.Length))
                .Select(name =>
                {
                    if (name == DefaultFeedName)
                        return null;

                    if (name.StartsWith(NamedFeedPrefix))
                        return name.Substring(NamedFeedPrefix.Length);

                    throw new ArgumentOutOfRangeException();
                });
        }
    }

    internal class AzureStorageFeed : IStorageFeed
    {
        private readonly string feedName;
        private readonly CloudTable table;
        private readonly CloudBlobContainer container;
        private readonly AzureQueryCache<PackageName> queryCache;
        private readonly PackageTable packageTable;
        private readonly PackageBlobContainer packageContainer;
        private readonly SymbolBlobContainer symbolContainer;
        private readonly SourceBlobContainer sourceContainer;

        public AzureStorageFeed(string feedName, CloudTable table, CloudBlobContainer container, AzureQueryCache<PackageName> queryCache)
        {
            this.feedName = feedName;
            this.table = table;
            this.container = container;
            this.queryCache = queryCache;
            packageTable = new PackageTable(table);
            packageContainer = new PackageBlobContainer(container);
            symbolContainer = new SymbolBlobContainer(container);
            sourceContainer = new SourceBlobContainer(container);
        }

        public string Name
        {
            get { return feedName; }
        }

        public override string ToString()
        {
            return feedName;
        }

        public IEnumerable<string> QueryInternals()
        {
            return container.ListBlobs().Select(blob => string.Join("/", blob.Uri.Segments.Skip(2)));
        }

        public async Task<IEnumerable<PackageName>> LoopPackageNameSegments(Func<int,int,Task<IEnumerable<PackageName>>> task)
        {
            var result = new List<PackageName>();
            var skip = 0;
            const int take = 100;

            while (true)
            {
                var segment = await task(skip, take);
                result.AddRange(segment);

                if (result.Count < skip + take)
                    break;

                skip += take;
            }

            return result;
        }

        public Task<IEnumerable<PackageName>> QueryPackages(PackageState packageState)
        {
            return LoopPackageNameSegments(async (skip, take) => await QueryPackages(packageState, null, skip, take));
        }

        public Task<IEnumerable<PackageName>> QueryPackages(string userName, PackageState packageState)
        {
            return LoopPackageNameSegments(async (skip, take) => await QueryPackages(userName, packageState, null, skip, take));
        }

        public Task<IEnumerable<PackageName>> QueryPackages(PackageState packageState, string packageNamePrefix, int skip, int take)
        {
            return queryCache.Get(
                string.Format("{0}/{1}/{2}*", feedName, packageState, packageNamePrefix),
                skip, take, TimeSpan.FromMinutes(5),
                async (continuationToken, actualTake) =>
                {
                    var tableContinuationToken = (TableContinuationToken)continuationToken;
                    var segment = await packageTable.QuerySegmented(packageState, packageNamePrefix, tableContinuationToken, actualTake);
                    return new ResultSegment<PackageName, object>(segment.Results.Select(p => p.Name), segment.ContinuationToken);
                });
        }

        public Task<IEnumerable<PackageName>> QueryPackages(string userName, PackageState packageState, string packageNamePrefix, int skip, int take)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentNullException("userName");

            return queryCache.Get(
                string.Format("{0}/{1}/{2}/{3}/*", feedName, packageState, userName, packageNamePrefix), 
                skip, take, TimeSpan.FromMinutes(5),
                async (continuationToken, actualTake) =>
                {
                    var blobContinuationToken = (BlobContinuationToken) continuationToken;
                    var segment = await packageContainer.ListBlobsSegmented(packageState, userName, packageNamePrefix, blobContinuationToken, actualTake);
                    return new ResultSegment<PackageName,object>(segment.Results.Select(p => p.Name), segment.ContinuationToken);
                });
        }

        public IPackageStorageItem GetPackage(string userName, PackageState packageState, PackageName packageName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                userName = null;

            return new AzurePackageStorageItem(this, packageTable, packageContainer, userName, packageState, packageName);
        }

        public IPackageRelatedStorageItem GetSymbol(PackageName packageName, SymbolName symbolName)
        {
            return new AzurePackageRelatedStorageItem(this, packageName, symbolContainer.GetBlobReference(symbolName), new SymbolRelatedPackageTable(table, symbolName));
        }

        public IPackageRelatedStorageItem GetSource(PackageName packageName, SourceName sourceName)
        {
            return new AzurePackageRelatedStorageItem(this, packageName, sourceContainer.GetBlobReference(sourceName), new SourceRelatedPackageTable(table, sourceName));
        }

        public async Task<bool> Delete()
        {
            var tableExisted = await table.DeleteIfExistsAsync();
            var containerExisted = await container.DeleteIfExistsAsync();
            Debug.Assert(tableExisted == containerExisted);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return tableExisted || containerExisted;
        }
    }

    internal class AzurePackageStorageItem : IPackageStorageItem
    {
        private readonly AzureStorageFeed feed;
        private readonly PackageTable table;
        private readonly PackageBlobContainer container;
        private readonly string userName;
        private readonly PackageState packageState;
        private readonly PackageName packageName;

        public AzurePackageStorageItem(
            AzureStorageFeed feed,
            PackageTable table, PackageBlobContainer container, string userName,
            PackageState packageState, PackageName packageName)
        {
            this.feed = feed;
            this.table = table;
            this.container = container;
            this.userName = userName;
            this.packageState = packageState;
            this.packageName = packageName;
        }

        public IStorageFeed Feed
        {
            get { return feed; }
        }

        public PackageName Name
        {
            get { return packageName; }
        }

        public PackageState State
        {
            get { return packageState; }
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}/{2}", feed, packageState, packageName);
        }

        // ReSharper disable once ParameterHidesMember
        private AzureBlob GetPackageBlob(string userName)
        {
            if (userName == null)
                return null;

            return container.GetBlobReference(packageState, userName, packageName);
        }

        public bool CanGetUri
        {
            get { return false; }
        }

        public Task<Uri> GetUri()
        {
            throw new NotSupportedException();
        }

        public async Task<string> GetUserName()
        {
            var packageEntity = await table.RetrieveAsync(packageState, packageName);

            if (packageEntity != null)
            {
                if (userName == null)
                    return packageEntity.UserName;

                if (packageEntity.UserName != userName)
                    return null;
            }

            return userName;
        }

        public async Task<bool> Exists()
        {
            var packageBlob = GetPackageBlob(await GetUserName());

            if (packageBlob == null)
                return false;

            return await packageBlob.ExistsAsync();
        }

        public async Task<Stream> Get()
        {
            var packageBlob = GetPackageBlob(await GetUserName());

            if (packageBlob == null)
                return null;

            return await packageBlob.OpenReadAsync();
        }

        private async Task<AzureBlob> PreparePut()
        {
            if (userName == null)
                throw new InvalidOperationException();

            var packageBlob = GetPackageBlob(userName);
            var packageEntity = await table.RetrieveAsync(packageState, packageName).ConfigureAwait(false);

            if (packageEntity != null)
            {
                if (packageEntity.UserName != userName)
                {
                    var oldBlob = GetPackageBlob(packageEntity.UserName);
                    await oldBlob.DeleteIfExistsAsync();
                }
            }
            else
            {
                packageEntity = new PackageTableEntity();
                await table.CreateIfNotExistsAsync();
                await container.CreateIfNotExistsAsync();
            }

            packageEntity.UserName = userName;
            await table.InsertOrReplaceAsync(packageState, packageName, packageEntity);
            return packageBlob;
        }

        public async Task<Stream> Put()
        {
            var packageBlob = await PreparePut().ConfigureAwait(false);
            return await packageBlob.OpenWriteAsync();
        }

        public async Task Get(Stream target)
        {
            var packageBlob = GetPackageBlob(await GetUserName());

            if (packageBlob == null)
                throw new InvalidOperationException();

            await packageBlob.DownloadToStreamAsync(target);
        }

        public async Task Put(Stream source)
        {
            var packageBlob = await PreparePut();
            await packageBlob.UploadFromStreamAsync(source);
        }

        public async Task<bool> Delete()
        {
            var packageBlob = GetPackageBlob(await GetUserName());

            if (packageBlob == null)
                return false;

            var packageExisted = await packageBlob.DeleteIfExistsAsync();
            var entityExisted = await table.DeleteIfExistsAsync(packageState, packageName);
            DebugExtensions.Assert(packageExisted == entityExisted);

            return packageExisted || entityExisted;
        }

        public async Task<IPackageStorageItem> Move(PackageState newState, PackageName newName)
        {
            var otherItem = await Copy(newState, newName);

            if (otherItem == null)
                return null;

            await Delete();
            return otherItem;
        }

        public async Task<IPackageStorageItem> Copy(PackageState newState, PackageName newName)
        {
            if (!await Exists())
                return null;

            var newItem = await PrepareMoveOrCopy(newState, newName);
            var packageEntity = await table.RetrieveAsync(packageState, packageName);
            await newItem.table.InsertOrReplaceAsync(newState, newName, packageEntity);
            await newItem.GetPackageBlob(await GetUserName()).CopyFromAsync(GetPackageBlob(await GetUserName()));
            return newItem;
        }

        private async Task<AzurePackageStorageItem> PrepareMoveOrCopy(PackageState newPackageState, PackageName newPackageName)
        {
            return new AzurePackageStorageItem(feed, table, container, await GetUserName(), newPackageState, newPackageName);
        }
    }

    internal class AzureRelatedPackageNameSet : IStorageSet<PackageName>
    {
        private readonly RelatedPackageTable table;

        public AzureRelatedPackageNameSet(RelatedPackageTable table)
        {
            this.table = table;
        }

        public async Task Add(PackageName item)
        {
            //TODO: request even if table exists
            await table.CreateIfNotExistsAsync();

            await table.InsertOrReplaceAsync(item);
        }

        public async Task Remove(PackageName item)
        {
            await table.DeleteIfExistsAsync(item);
        }

        public async Task<IEnumerable<PackageName>> List()
        {
            return table.Query();
        }
    }

    internal class AzurePackageRelatedStorageItem : IPackageRelatedStorageItem
    {
        private readonly AzureStorageFeed feed;
        private readonly PackageName packageName;
        private readonly AzureBlob itemBlob;
        private readonly RelatedPackageTable relatedPackageTable;

        public AzurePackageRelatedStorageItem(AzureStorageFeed feed, PackageName packageName, AzureBlob itemBlob, RelatedPackageTable relatedPackageTable)
        {
            this.feed = feed;
            this.packageName = packageName;
            this.itemBlob = itemBlob;
            this.relatedPackageTable = relatedPackageTable;
        }

        public IStorageFeed Feed
        {
            get { return feed; }
        }

        public bool CanGetUri
        {
            get { return false; }
        }

        public async Task<bool> Exists()
        {
            return await itemBlob.ExistsAsync();
        }

        public Task<Uri> GetUri()
        {
            throw new NotSupportedException();
        }

        public Task<Stream> Get()
        {
            return itemBlob.OpenReadAsync();
        }

        private async Task PreparePut()
        {
            if (packageName == null)
                throw new InvalidOperationException();

            //TODO: request even if container exists
            await itemBlob.CreateContainerIfNotExistsAsync();

            await PackageNames.Add(packageName);
        }

        public async Task<Stream> Put()
        {
            await PreparePut();
            return await itemBlob.OpenWriteAsync();
        }

        public async Task Get(Stream target)
        {
            await itemBlob.DownloadToStreamAsync(target);
        }

        public async Task Put(Stream source)
        {
            await PreparePut();
            await itemBlob.UploadFromStreamAsync(source);
        }

        public async Task<bool> Delete()
        {
            if (packageName == null)
                throw new InvalidOperationException();

            await PackageNames.Remove(packageName);

            if (!(await PackageNames.List()).Any())
                return await itemBlob.DeleteIfExistsAsync();

            return false;
        }

        public IStorageSet<PackageName> PackageNames
        {
            get { return new AzureRelatedPackageNameSet(relatedPackageTable); }
        }
    }
}