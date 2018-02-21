using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using SymbolSource.Contract.Storage.Local;

namespace SymbolSource.Contract.Storage
{
    public class LocalStorageService : IStorageService
    {
        private readonly ILocalStorageConfiguration configuration;

        public LocalStorageService(ILocalStorageConfiguration configuration)
        {
            this.configuration = configuration;
        }

        private string GetFeedPath(string feedName)
        {
            if (string.IsNullOrEmpty(feedName))
                feedName = "_";

            return Path.Combine(configuration.RootPath, feedName);
        }

        public IStorageFeed GetFeed(string feedName)
        {
            return new FileStorageFeed(feedName, GetFeedPath(feedName));
        }

        public IEnumerable<string> QueryFeeds()
        {
            //materializing to avoid directory enumeration handles locking deletes
            return Directory.EnumerateDirectories(configuration.RootPath)
                .Select(path => path == "_" ? null : path)
                .ToList();
        }
    }

    internal class FileStorageFeed : IStorageFeed
    {
        private readonly string feedName;
        private readonly string feedPath;

        public FileStorageFeed(string feedName, string feedPath)
        {
            this.feedName = feedName;
            this.feedPath = feedPath;
        }

        public string Name
        {
            get { return feedName; }
        }

        public override string ToString()
        {
            return feedName;
        }

        internal string Path
        {
            get { return feedPath; }
        }

        public IEnumerable<string> QueryInternals()
        {
            //materializing to avoid directory enumeration handles locking deletes
            return Directory.EnumerateFileSystemEntries(feedPath)
                .Select(path => path.Substring(feedPath.Length))
                .ToList();
        }

        public async Task<IEnumerable<PackageName>> QueryPackages(PackageState packageState)
        {
            return LocalPackageStorageItem.Query(feedPath, packageState).ToList();
        }

        public async Task<IEnumerable<PackageName>> QueryPackages(string userName, PackageState packageState)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentNullException("userName");

            //materializing to avoid directory enumeration handles locking deletes
            return LocalPackageStorageItem.Query(feedPath, userName, packageState).ToList();
        }

        public async Task<IEnumerable<PackageName>> QueryPackages(PackageState packageState, string packageNamePrefix, int skip, int take)
        {
            return (await QueryPackages(packageState)).Skip(skip).Take(take);
        }

        public async Task<IEnumerable<PackageName>> QueryPackages(string userName, PackageState packageState, string packageNamePrefix, int skip, int take)
        {
            return (await QueryPackages(userName, packageState)).Skip(skip).Take(take);
        }

        public IPackageStorageItem GetPackage(string userName, PackageState packageState, PackageName packageName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                userName = null;

            return new LocalPackageStorageItem(this, userName, packageState, packageName);
        }

        public IPackageRelatedStorageItem GetSymbol(PackageName packageName, SymbolName symbolName)
        {
            return new LocalSymbolStorageItem(this, packageName, symbolName);
        }

        public IPackageRelatedStorageItem GetSource(PackageName packageName, SourceName sourceName)
        {
            return new LocalSourceStorageItem(this, packageName, sourceName);
        }

        public async Task<bool> Delete()
        {
            if (!Directory.Exists(feedPath))
                return false;

            Directory.Delete(feedPath, true);
            return true;
        }
    }

    internal class LocalPackageStorageItem : IPackageStorageItem
    {
        private const int PackagePathDepth = 6;
        private const int LinkPathDepth = 4;

        private static string GetPackageRelativePath(PackageName packageName, string extension)
        {
            return Path.Combine(packageName.Id.Substring(0, 1), packageName.Id, packageName.Version,
                string.Format("{0}.{1}.{2}", packageName.Id, packageName.Version, extension));
        }

        private static string GetStatePath(string feedPath, PackageState packageState)
        {
            return Path.Combine(feedPath, "." + packageState.ToString().ToLower());
        }

        private static string GetUserPath(string statePath, string userName)
        {
            return Path.Combine(statePath, ".users", userName);
        }

        private static void EnsureParent(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }

        public static IEnumerable<PackageName> Query(string feedPath, PackageState packageState)
        {
            return Query(feedPath, null, packageState);
        }

        public static IEnumerable<PackageName> Query(string feedPath, string userName, PackageState packageState)
        {
            var path = GetStatePath(feedPath, packageState);

            if (userName != null)
                path = GetUserPath(path, userName);

            if (!Directory.Exists(path))
                return new PackageName[0];

            return Directory.EnumerateDirectories(path)
                .Select(Path.GetFileName)
                .Where(letter => !letter.StartsWith("."))
                .SelectMany(letter => Directory.EnumerateDirectories(Path.Combine(path, letter))
                    .SelectMany(idPath => Directory.EnumerateDirectories(idPath)
                        .Select(versionPath => new PackageName(Path.GetFileName(idPath), Path.GetFileName(versionPath)))));
        }

        private readonly FileStorageFeed feed;
        private readonly PackageState packageState;
        private readonly PackageName packageName;

        private readonly string statePath;
        private readonly LinkFile linkFile;
        private readonly string packageRelativePath;
        private readonly string userName;

        public LocalPackageStorageItem(
            FileStorageFeed feed, string userName,
            PackageState packageState, PackageName packageName)
        {
            this.feed = feed;
            this.packageState = packageState;
            this.packageName = packageName;

            statePath = GetStatePath(feed.Path, packageState);
            packageRelativePath = GetPackageRelativePath(packageName, "nupkg");

            linkFile = new LinkFile(Path.Combine(statePath, GetPackageRelativePath(packageName, "txt")), LinkPathDepth);
            this.userName = userName;
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
            get { throw new NotImplementedException(); }
        }

        // ReSharper disable once ParameterHidesMember
        private PackageFile GetPackageFile(string userName)
        {
            if (userName == null)
                return null;

            return new PackageFile(Path.Combine(GetUserPath(statePath, userName), packageRelativePath), PackagePathDepth);
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
            var packageEntity = await linkFile.Retrieve();

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
            var packageFile = GetPackageFile(await GetUserName());

            if (packageFile == null)
                return false;

            return packageFile.Exists();
        }

        public async Task<Stream> Get()
        {
            var packagePath = GetPackageFile(await GetUserName());

            if (packagePath == null)
                return null;

            return packagePath.OpenRead();
        }

        public async Task<Stream> Put()
        {
            if (userName == null)
                throw new InvalidOperationException();

            var packageFile = GetPackageFile(userName);
            var packageEntity = await linkFile.Retrieve().ConfigureAwait(false);

            if (packageEntity != null)
            {
                if (packageEntity.UserName != userName)
                {
                    var oldPackageFile = GetPackageFile(packageEntity.UserName);
                    oldPackageFile.MoveTo(packageFile);
                }
            }
            else
            {
                packageEntity = new LinkFileEntity();
                linkFile.EnsureParent();
                packageFile.EnsureParent();
            }

            packageEntity.UserName = userName;
            await linkFile.Store(packageEntity).ConfigureAwait(false);
            return packageFile.OpenWrite();
        }

        public async Task Get(Stream target)
        {
            using (var source = await Get())
            {
                if (source == null)
                    throw new InvalidOperationException();

                //await source.CopyToAsync(source);
                source.CopyTo(target);
            }
        }

        public async Task Put(Stream source)
        {
            using (var target = await Put())
                //await source.CopyToAsync(source);
                source.CopyTo(target);
        }

        public async Task<bool> Delete()
        {
            var packageFile = GetPackageFile(await GetUserName());

            if (packageFile == null)
                return false;

            var packageExisted = packageFile.Delete();
            var linkExisted = linkFile.Delete();

            DebugExtensions.Assert(packageExisted == linkExisted);

            return packageExisted || linkExisted;
        }

        private async Task<LocalPackageStorageItem> PrepareMoveOrCopy(PackageState packageState, PackageName packageName)
        {
            return new LocalPackageStorageItem(feed, await GetUserName(), packageState, packageName);
        }

        public async Task<IPackageStorageItem> Move(PackageState newState, PackageName newName)
        {
            if (!await Exists())
                return null;

            var newItem = await PrepareMoveOrCopy(newState, newName);
            GetPackageFile(await GetUserName()).MoveTo(newItem.GetPackageFile(await newItem.GetUserName()));
            linkFile.MoveTo(newItem.linkFile);
            await Delete();
            return newItem;
        }

        public async Task<IPackageStorageItem> Copy(PackageState newState, PackageName newName)
        {
            if (!await Exists())
                return null;

            var newItem = await PrepareMoveOrCopy(newState, newName);
            GetPackageFile(await GetUserName()).CopyTo(newItem.GetPackageFile(await newItem.GetUserName()));
            linkFile.CopyTo(newItem.linkFile);
            return newItem;
        }
    }

    internal class LocalPackageNameSet : IStorageSet<PackageName>
    {
        private readonly string packagesPath;
        private readonly int deleteDepth;

        public LocalPackageNameSet(string packagesPath, int deleteDepth)
        {
            this.packagesPath = packagesPath;
            this.deleteDepth = deleteDepth;
        }

        private string GetPackagePath(PackageName item)
        {
            return Path.Combine(packagesPath, item.Id, string.Format("{0}.txt", item.Version));
        }

        public async Task Add(PackageName item)
        {
            var file = new PackageFile(GetPackagePath(item), deleteDepth);
            file.EnsureParent();
            file.OpenWrite().Dispose();
        }

        public async Task Remove(PackageName item)
        {
            var file = new PackageFile(GetPackagePath(item), deleteDepth);
            file.Delete();
        }

        public async Task<IEnumerable<PackageName>> List()
        {
            if (!Directory.Exists(packagesPath))
                return new PackageName[0];

            return Directory.EnumerateDirectories(packagesPath)
                .SelectMany(idPath => Directory.EnumerateFiles(idPath)
                    .Select(versionPath => new PackageName(Path.GetFileName(idPath), Path.GetFileNameWithoutExtension(versionPath))));
        }
    }

    internal class LocalPackageRelatedStorageItem : IPackageRelatedStorageItem
    {
        private readonly FileStorageFeed feed;
        protected readonly PackageName packageName;
        private readonly string rootPath;
        private readonly StorageFile itemFile;

        public LocalPackageRelatedStorageItem(FileStorageFeed feed, PackageName packageName, int deleteDepth, string rootPath, string itemName)
        {
            this.feed = feed;
            this.packageName = packageName;
            this.rootPath = rootPath;
            itemFile = new StorageFile(Path.Combine(rootPath, itemName), deleteDepth);
        }

        public IStorageFeed Feed
        {
            get { return feed; }
        }

        public bool CanGetUri
        {
            get { return false; }
        }

        public Task<Uri> GetUri()
        {
            throw new NotSupportedException();
        }

        public async Task<bool> Exists()
        {
            return itemFile.Exists();
        }

        public async Task<Stream> Get()
        {
            return itemFile.OpenRead();
        }

        public async Task<Stream> Put()
        {
            if (packageName == null)
                throw new InvalidOperationException();

            await PackageNames.Add(packageName);

            itemFile.EnsureParent();
            return itemFile.OpenWrite();
        }

        public async Task Get(Stream target)
        {
            using (var source = await Get())
            {
                if (source == null)
                    throw new InvalidOperationException();

                //await source.CopyToAsync(source);
                source.CopyTo(target);
            }
        }

        public async Task Put(Stream source)
        {
            using (var target = await Put())
                //await source.CopyToAsync(source);
                source.CopyTo(target);
        }

        public async Task<bool> Delete()
        {
            if (packageName == null)
                throw new InvalidOperationException();

            await PackageNames.Remove(packageName);

            if (!(await PackageNames.List()).Any())
                return itemFile.Delete();

            return false;
        }

        public IStorageSet<PackageName> PackageNames
        {
            get { return new LocalPackageNameSet(rootPath, 1); }
        }
    }

    internal class LocalSymbolStorageItem : LocalPackageRelatedStorageItem
    {
        private static string GetSymbolPath(string feedPath, SymbolName symbolName)
        {
            return Path.Combine(feedPath, symbolName.ImageName.Substring(0, 1), symbolName.ImageName + ".pdb", symbolName.SymbolHash);
        }

        public LocalSymbolStorageItem(FileStorageFeed feed, PackageName packageName, SymbolName symbolName)
            : base(feed, packageName, 4, GetSymbolPath(feed.Path, symbolName), symbolName.ImageName + ".pd_")
        {
        }
    }

    internal class LocalSourceStorageItem : LocalPackageRelatedStorageItem
    {
        private static string GetSourcePath(string feedPath, SourceName sourceName)
        {
            return Path.Combine(feedPath, sourceName.FileName.Substring(0, 1), sourceName.FileName, sourceName.Hash);
        }

        public LocalSourceStorageItem(FileStorageFeed feed, PackageName packageName, SourceName sourceName)
            : base(feed, packageName, 4, GetSourcePath(feed.Path, sourceName), sourceName.FileName)
        {
        }
    }
}