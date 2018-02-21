using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace SymbolSource.Contract.Storage.Azure
{
    internal class AzureBlob
    {
        protected CloudBlockBlob blob;

        public AzureBlob(CloudBlockBlob blob)
        {
            this.blob = blob;
        }

        public async Task<bool> CreateContainerIfNotExistsAsync()
        {
            return await blob.Container.CreateIfNotExistsAsync();
        }

        public async Task<bool> ExistsAsync()
        {
            return await blob.ExistsAsync();
        }

        public async Task<Stream> OpenReadAsync()
        {
            try
            {
                return await blob.OpenReadAsync();
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 404)
                    return null;

                throw;
            }
        }

        public async Task<Stream> OpenWriteAsync()
        {
            try
            {
                return await blob.OpenWriteAsync();
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 404)
                    return null;

                throw;
            }
        }

        public async Task<bool> DeleteIfExistsAsync()
        {
            return await blob.DeleteIfExistsAsync();
        }

        public async Task DownloadToStreamAsync(Stream target)
        {
            await blob.DownloadToStreamAsync(target);
        }

        public async Task UploadFromStreamAsync(Stream source)
        {
            await blob.UploadFromStreamAsync(source);
        }

        public async Task CopyFromAsync(AzureBlob source)
        {
            await blob.StartCopyAsync(source.blob);
            var start = DateTime.Now;
            var timeout = TimeSpan.FromMinutes(2);
            var delay = TimeSpan.FromSeconds(1);

            while (blob.CopyState.Status == CopyStatus.Pending && (DateTime.Now - start) < timeout)
            {
                await Task.Delay(delay);
            }

            if (blob.CopyState.Status != CopyStatus.Success)
                throw new InvalidOperationException(blob.CopyState.StatusDescription);
        }
    }

    internal class ResultSegment<TItem, TToken>
    {
        public ResultSegment(IEnumerable<TItem> results, TToken continuationToken)
        {
            Results = results;
            ContinuationToken = continuationToken;
        }

        public IEnumerable<TItem> Results { get; private set; }
        public TToken ContinuationToken { get; private set; }
    }

    internal class PackageBlobResultSegment : ResultSegment<PackageBlob, BlobContinuationToken>
    {
        public PackageBlobResultSegment(IEnumerable<PackageBlob> results, BlobContinuationToken continuationToken) 
            : base(results, continuationToken)
        {
        }
    }

    internal class PackageBlob
    {
        public PackageBlob(PackageName name, AzureBlob blob)
        {
            Name = name;
            Blob = blob;
        }

        public PackageName Name { get; private set; }
        public AzureBlob Blob { get; private set; }
    }

    internal class PackageBlobContainer
    {
        private readonly CloudBlobContainer container;

        public PackageBlobContainer(CloudBlobContainer container)
        {
            this.container = container;
        }

        public async Task<bool> CreateIfNotExistsAsync()
        {
            return await container.CreateIfNotExistsAsync();
        }

        private static string GetDirectoryPath(PackageState packageState, string userName)
        {
            return string.Format("pkg/{0}/{1}", packageState.ToString().ToLower(), userName);
        }

        private static string GetPackagePath(PackageName packageName)
        {
            return string.Format("{0}/{1}/{0}.{1}.nupkg", packageName.Id, packageName.Version);
        }

        private static string GetPath(PackageState packageState, string userName, PackageName packageName)
        {
            return string.Format("{0}/{1}", GetDirectoryPath(packageState, userName), GetPackagePath(packageName));
        }

        public PackageName GetPackageName(string blobName)
        {
            var parts = blobName.Split('/');

            if (parts[0] != "pkg")
                throw new InvalidOperationException();

            return new PackageName(parts[3], parts[4]);
        }

        public AzureBlob GetBlobReference(PackageState packageState, string userName, PackageName packageName)
        {
            var path = GetPath(packageState, userName, packageName);
            return new AzureBlob(container.GetBlockBlobReference(path));
        }

        public async Task<PackageBlobResultSegment> ListBlobsSegmented(PackageState packageState, string userName, string packageNamePrefix, BlobContinuationToken continuationToken, int take)
        {
            try
            {
                var pathPrefix = Path.Combine(GetDirectoryPath(packageState, userName), packageNamePrefix ?? "");
                var segment = await container.ListBlobsSegmentedAsync(pathPrefix, true, BlobListingDetails.None, take, continuationToken, null, null);

                var results = segment.Results
                    .OfType<CloudBlockBlob>()
                    .Select(blob => new PackageBlob(GetPackageName(blob.Name), new AzureBlob(blob)));

                return new PackageBlobResultSegment(results, segment.ContinuationToken);
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 404)
                    return new PackageBlobResultSegment(new PackageBlob[0], null);

                throw;
            }
        }
    }

    internal class SymbolBlobContainer
    {
        private readonly CloudBlobContainer container;

        public SymbolBlobContainer(CloudBlobContainer container)
        {
            this.container = container;
        }

        private static string GetPath(SymbolName symbolName)
        {
            return string.Format("pdb/{0}/{1}", symbolName.ImageName, symbolName.SymbolHash);
        }

        public AzureBlob GetBlobReference(SymbolName symbolName)
        {
            return new AzureBlob(container.GetBlockBlobReference(GetPath(symbolName)));
        }
    }

    internal class SourceBlobContainer
    {
        private readonly CloudBlobContainer container;

        public SourceBlobContainer(CloudBlobContainer container)
        {
            this.container = container;
        }

        private static string GetPath(SourceName sourceName)
        {
            return string.Format("src/{0}/{1}", sourceName.FileName, sourceName.Hash);
        }

        public AzureBlob GetBlobReference(SourceName sourceName)
        {
            return new AzureBlob(container.GetBlockBlobReference(GetPath(sourceName)));
        }
    }
}