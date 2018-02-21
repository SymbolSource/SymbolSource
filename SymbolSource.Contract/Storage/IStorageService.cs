using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SymbolSource.Contract.Storage
{
    public interface IStorageItem
    {
        IStorageFeed Feed { get; }

        bool CanGetUri { get; }
        Task<bool> Exists();
        Task<Uri> GetUri();
        Task<Stream> Get();
        Task<Stream> Put();
        Task Get(Stream target);
        Task Put(Stream source);
        Task<bool> Delete();
    }

    public interface IPackageStorageItem : IStorageItem
    {
        PackageName Name { get; }
        PackageState State { get; }

        Task<string> GetUserName();
        Task<IPackageStorageItem> Move(PackageState newState, PackageName newName);
        Task<IPackageStorageItem> Copy(PackageState newState, PackageName newName);
    }

    public interface IStorageSet<T>
    {
        Task Add(T item);
        Task Remove(T item);

        Task<IEnumerable<T>> List();
    }

    public interface IPackageRelatedStorageItem : IStorageItem
    {
        IStorageSet<PackageName> PackageNames { get; } 
    }

    public interface IStorageFeed
    {
        string Name { get; }

        IEnumerable<string> QueryInternals();

        Task<IEnumerable<PackageName>> QueryPackages(PackageState packageState);
        Task<IEnumerable<PackageName>> QueryPackages(string userName, PackageState packageState);

        Task<IEnumerable<PackageName>> QueryPackages(PackageState packageState, string packageNamePrefix, int skip, int take);
        Task<IEnumerable<PackageName>> QueryPackages(string userName, PackageState packageState, string packageNamePrefix, int skip, int take);

        IPackageStorageItem GetPackage(string userName, PackageState packageState, PackageName packageName);

        IPackageRelatedStorageItem GetSymbol(PackageName packageName, SymbolName symbolName);
        IPackageRelatedStorageItem GetSource(PackageName packageName, SourceName sourceName);

        Task<bool> Delete();
    }

    public interface IStorageService
    {
        IStorageFeed GetFeed(string feedName);
        IEnumerable<string> QueryFeeds();
    }
}