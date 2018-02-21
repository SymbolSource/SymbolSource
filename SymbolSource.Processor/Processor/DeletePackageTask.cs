using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NuGet;
using SymbolSource.Contract;
using SymbolSource.Contract.Storage;
using SymbolSource.Contract.Support;
using SymbolSource.Processor.Notifier;
using PackageName = SymbolSource.Contract.PackageName;

namespace SymbolSource.Processor.Processor
{
    public class DeletePackageTask : PackageTask
    {
        private readonly INotifierService notifier;
        private readonly ISupportService support;

        public DeletePackageTask(
            INotifierService notifier,
            ISupportService support)
        {
            this.notifier = notifier;
            this.support = support;
        }

        public async Task DeletePackage(UserInfo userInfo, IStorageFeed feed, PackageName packageName, IPackageStorageItem packageItem, IPackage package)
        {
            var statusPackageBuilder = CreatePackage(packageName, package);
            var statusPackageState = PackageState.Partial;

            try
            {
                var allRead = true;
                var statusFiles = new List<ImageStatusFile>();

                foreach (var statusPackageFile in package.GetFiles().Where(f => f.Path.EndsWith(".smbsrc")))
                {
                    try
                    {
                        var imageStatus = ReadFile<ImageStatus>(statusPackageFile);
                        statusFiles.Add(new ImageStatusFile(statusPackageFile.Path, imageStatus));
                    }
                    catch (Exception e)
                    {
                        support.TrackException(e, new { packageName });
                        Trace.TraceError("Error while deserializing status {0}:\n{1}", packageName, e);
                        allRead = false;
                        statusPackageBuilder.Files.Add(statusPackageFile);
                    }
                }

                var newStatusFiles = await ProcessThrottled(
                    2, statusFiles,
                    async s => await DeleteImage(feed, packageName, s));

                foreach (var statusFile in newStatusFiles)
                    statusPackageBuilder.Files.Add(CreateFile(statusFile.FilePath, statusFile.ImageStatus));

                if (allRead && newStatusFiles.All(s => s.ImageStatus.Check(false)))
                    statusPackageState = PackageState.Deleted;

                Trace.TraceInformation("Marking package {0} as {1}", packageName, statusPackageState);
            }
            catch (Exception e)
            {
                support.TrackException(e, new { packageName });
                Trace.TraceError("Error while deleting package {0}:\n{1}", packageName, e);
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
                    await notifier.PartiallyDeleted(userInfo, packageName);
                    break;
                case PackageState.Deleted:
                    await notifier.Deleted(userInfo, packageName);
                    break;
                default:
                    // ReSharper disable once NotResolvedInText
                    throw new ArgumentOutOfRangeException("statusPackageState", statusPackageState, null);
            }
        }

        private async Task<ImageStatusFile> DeleteImage(IStorageFeed feed, PackageName packageName, ImageStatusFile statusFile)
        {
            var newStatusFile = new ImageStatusFile(statusFile.FilePath, new ImageStatus(statusFile.ImageStatus.ImageName));

            //try
            //{
            //    //Debug.WriteLine("Deleting image {0}", statusFile.ImageStatus.ImageName);
            //    //statusFile.ImageStatus.Stored = false;
            //    statusFile.ImageStatus.Exception = null;
            //    //Debug.WriteLine("Deleted image {0}", statusFile.ImageStatus.ImageName);
            //}
            //catch (Exception e)
            //{
            //    support.TrackException(e);
            //    newStatusFile.ImageStatus.Exception = new ExceptionStatus(e);
            //}

            if (statusFile.ImageStatus.SymbolStatus != null)
                newStatusFile.ImageStatus.SymbolStatus = await DeleteSymbol(feed, packageName, statusFile.ImageStatus.SymbolStatus);

            return newStatusFile;
        }

        private async Task<SymbolStatus> DeleteSymbol(IStorageFeed feed, PackageName packageName, SymbolStatus symbolStatus)
        {
            var newSymbolStatus = new SymbolStatus(symbolStatus.SymbolName);

            if (symbolStatus.Stored)
            {
                try
                {
                    Debug.WriteLine("Deleting symbol {0}", symbolStatus.SymbolName);
                    newSymbolStatus.Stored = true;

                    await RequestOrSkip(string.Format("pdb/{0}", symbolStatus.SymbolName),
                        async () =>
                        {
                            var symbolItem = feed.GetSymbol(packageName, symbolStatus.SymbolName);
                            await symbolItem.Delete();
                        });

                    newSymbolStatus.Stored = false;
                    Debug.WriteLine("Deleted symbol {0}", symbolStatus.SymbolName);
                }
                catch (Exception e)
                {
                    support.TrackException(e, new { packageName });
                    newSymbolStatus.Exception = new ExceptionStatus(e);
                }
            }

            if (symbolStatus.SourceStatuses != null)
                newSymbolStatus.SourceStatuses = await ProcessThrottled(
                    5, symbolStatus.SourceStatuses,
                    async s => await DeleteSource(feed, packageName, s));

            return newSymbolStatus;
        }

        private async Task<SourceStatus> DeleteSource(IStorageFeed feed, PackageName packageName, SourceStatus sourceStatus)
        {
            var newSourceStatus = new SourceStatus(sourceStatus.SourceName);

            if (sourceStatus.Stored)
            {
                try
                {
                    Debug.WriteLine("Deleting source {0}", sourceStatus.SourceName);
                    newSourceStatus.Stored = true;

                    await RequestOrSkip(string.Format("src/{0}", sourceStatus.SourceName),
                        async () =>
                        {
                            var source = feed.GetSource(packageName, sourceStatus.SourceName);
                            await source.Delete();
                        });

                    newSourceStatus.Stored = false;
                    Debug.WriteLine("Deleted source {0}", sourceStatus.SourceName);
                }
                catch (Exception e)
                {
                    support.TrackException(e, new { packageName });
                    newSourceStatus.Exception = new ExceptionStatus(e);
                }
            }

            return newSourceStatus;
        }
    }
}