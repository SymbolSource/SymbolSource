using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet;
using SymbolSource.Contract;
using SymbolSource.Contract.Storage;
using SymbolSource.Contract.Support;
using SymbolSource.Processor.Legacy;
using SymbolSource.Processor.Legacy.Projects;
using SymbolSource.Processor.Notifier;
using PackageName = SymbolSource.Contract.PackageName;

namespace SymbolSource.Processor.Processor
{
    public class IndexPackageTask : PackageTask
    {
        private readonly IPackageProcessorConfiguration configuration;
        private readonly INotifierService notifier;
        private readonly ISupportService support;
        private readonly IAddInfoBuilder addInfoBuilder;
        private readonly IPdbStoreManager pdbStoreManager;
        private readonly IFileCompressor fileCompressor;

        public IndexPackageTask(
            IPackageProcessorConfiguration configuration,
            INotifierService notifier,
            ISupportService support, 
            IAddInfoBuilder addInfoBuilder, 
            IPdbStoreManager pdbStoreManager, 
            IFileCompressor fileCompressor)
        {
            this.configuration = configuration;
            this.notifier = notifier;
            this.support = support;
            this.addInfoBuilder = addInfoBuilder;
            this.pdbStoreManager = pdbStoreManager;
            this.fileCompressor = fileCompressor;
        }

        public async Task IndexPackage(UserInfo userInfo, IStorageFeed feed, PackageName packageName, IPackageStorageItem packageItem, ZipPackage package)
        {
            var statusPackageBuilder = CreatePackage(packageName, package);
            var statusPackageState = PackageState.Partial;

            try
            {
                var statusFiles = await ProcessThrottled(
                    2, addInfoBuilder.Build(new NugetPackage(package)),
                    async i => await IndexImage(feed, packageName, i));

                foreach (var statusFile in statusFiles)
                    statusPackageBuilder.Files.Add(CreateFile(statusFile.FilePath, statusFile.ImageStatus));

                if (statusFiles.All(s => s.ImageStatus.Check(true)))
                    statusPackageState = PackageState.Succeded;

                Trace.TraceInformation("Marking package {0} as {1}", packageName, statusPackageState);
            }
            catch (Exception e)
            {
                support.TrackException(e, new { packageName });
                Trace.TraceError("Error while indexing package {0}:\n{1}", packageName, e);
                statusPackageBuilder.Files.Add(CreateFile("error.txt", e.ToString()));
            }

            if (statusPackageBuilder.Files.Count == 0)
                statusPackageBuilder.Files.Add(CreateFile("empty.txt", string.Empty));

            Debug.WriteLine("Saving package processing status {0}", packageName);
            var statusPackageItem = feed.GetPackage(await packageItem.GetUserName(), statusPackageState, packageName);

            using (var statusStream = await statusPackageItem.Put())
                statusPackageBuilder.SaveBuffered(statusStream);

            switch (statusPackageState)
            {
                case PackageState.Partial:
                    await notifier.PartiallyIndexed(userInfo, packageName);
                    break;
                case PackageState.Succeded:
                    await notifier.Indexed(userInfo, packageName);
                    break;
                default:
                    // ReSharper disable once NotResolvedInText
                    throw new ArgumentOutOfRangeException("statusPackageState", statusPackageState, null);
            }
        }

        private PdbSrcSrvSection CreatePdbStrSection(IBinaryInfo binaryInfo)
        {
            var pdbstr = new PdbSrcSrvSection();

            pdbstr.Ini.Add("VERSION", "2");
            pdbstr.Ini.Add("INDEXVERSION", "2");
            pdbstr.Variables.Add("SRCSRVTRG", configuration.ServerUrl + "/src/%fnfile%(%var1%)/%var2%/%fnfile%(%var1%)");
            pdbstr.Variables.Add("SRCSRVCMD", string.Empty);
            pdbstr.Variables.Add("SRCSRVVERCTRL", "http");
            pdbstr.Ini.Add("VERCTRL", "http");

            foreach (var source in binaryInfo.SymbolInfo.SourceInfos)
                pdbstr.Sources.Add(new[] { source.OriginalPath, source.Hash });

            return pdbstr;
        }

        private async Task<ImageStatusFile> IndexImage(IStorageFeed feed, PackageName packageName, IBinaryInfo binaryInfo)
        {
            var statusFile = new ImageStatusFile(binaryInfo.File.FullPath, new ImageStatus(new ImageName(binaryInfo.Name, binaryInfo.Hash)));

            //try
            //{
            //    //Debug.WriteLine("Indexing image {0}", imageStatus.ImageName);
            //    //imageStatus.Stored = true;
            //    //Debug.WriteLine("Stored image {0}", imageStatus.ImageName);
            //}
            //catch (Exception e)
            //{
            //    support.TrackException(e);
            //    statusFile.ImageStatus.Exception = new ExceptionStatus(e);
            //}

            if (binaryInfo.SymbolInfo != null)
                statusFile.ImageStatus.SymbolStatus = await IndexSymbol(feed, packageName, binaryInfo);

            return statusFile;
        }

        private async Task<SymbolStatus> IndexSymbol(IStorageFeed feed, PackageName packageName, IBinaryInfo binaryInfo)
        {
            var symbolStatus = new SymbolStatus(new SymbolName(binaryInfo.Name, binaryInfo.SymbolInfo.Hash));

            try
            {
                Debug.WriteLine("Indexing symbol {0}", symbolStatus.SymbolName);
                await RequestOrSkip(string.Format("pdb/{0}", symbolStatus.SymbolName),
                    async () =>
                    {
                        Debug.WriteLine("Storing symbol {0}", symbolStatus.SymbolName);
                        var symbolItem = feed.GetSymbol(packageName, symbolStatus.SymbolName);

                        var pdbstrSection = CreatePdbStrSection(binaryInfo);
                        using (var inputStream = binaryInfo.SymbolInfo.File.GetStream())
                        using (var tempStream = new MemoryStream())
                        using (var outputStream = await symbolItem.Put())
                        {
                            pdbStoreManager.WriteSrcSrv(inputStream, tempStream, pdbstrSection);
                            tempStream.Position = 0;
                            var symbolFileName = Path.GetFileName(binaryInfo.SymbolInfo.File.FullPath);
                            fileCompressor.Compress(symbolFileName, tempStream, outputStream);
                        }
                    });

                symbolStatus.Stored = true;
                Debug.WriteLine("Stored symbol {0}", symbolStatus.SymbolName);
            }
            catch (Exception e)
            {
                support.TrackException(e, new { packageName });
                symbolStatus.Exception = new ExceptionStatus(e);
            }

            if (binaryInfo.SymbolInfo.SourceInfos != null)
                symbolStatus.SourceStatuses = await ProcessThrottled(
                    5, binaryInfo.SymbolInfo.SourceInfos,
                    async s => await IndexSource(feed, packageName, s));

            return symbolStatus;
        }

        private async Task<SourceStatus> IndexSource(IStorageFeed feed, PackageName packageName, ISourceInfo sourceInfo)
        {
            var sourceStatus = new SourceStatus(new SourceName(Path.GetFileName(sourceInfo.OriginalPath), sourceInfo.Hash));

            try
            {
                Debug.WriteLine("Indexing source {0}", sourceStatus.SourceName);
                await RequestOrSkip(string.Format("src/{0}", sourceStatus.SourceName),
                    async () =>
                    {
                        using (var stream = sourceInfo.File.GetStream())
                        using (var convertedStream = SourceConverter.Convert(stream))
                            await feed.GetSource(packageName, sourceStatus.SourceName).Put(convertedStream);
                    });

                sourceStatus.Stored = true;
                Debug.WriteLine("Stored source {0}", sourceStatus.SourceName);
            }
            catch (Exception e)
            {
                support.TrackException(e, new { packageName });
                sourceStatus.Exception = new ExceptionStatus(e);
            }

            return sourceStatus;
        }
    }
}