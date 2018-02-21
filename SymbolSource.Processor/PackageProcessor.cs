using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NuGet;
using SymbolSource.Contract;
using SymbolSource.Contract.Processor;
using SymbolSource.Contract.Scheduler;
using SymbolSource.Contract.Storage;
using SymbolSource.Contract.Support;
using SymbolSource.Processor.Legacy;
using SymbolSource.Processor.Legacy.Projects;
using SymbolSource.Processor.Notifier;
using SymbolSource.Processor.Processor;
using IPackageFile = NuGet.IPackageFile;
using PackageName = SymbolSource.Contract.PackageName;

namespace SymbolSource.Processor
{
    internal class ExceptionStatus
    {
        public ExceptionStatus(Exception exception)
        {
            if (exception != null)
            {
                Message = exception.Message;
                StackTrace = exception.StackTrace;
            }
        }

        public string Message { get; private set; }
        public string StackTrace { get; private set; }
    }

    internal class SourceStatus
    {
        public SourceName SourceName { get; private set; }
        public bool Stored { get; set; }
        public ExceptionStatus Exception { get; set; }

        public SourceStatus(SourceName sourceName)
        {
            SourceName = sourceName;
        }

        public bool Check(bool expectedStored)
        {
            return Stored == expectedStored && Exception == null;
        }
    }

    internal class SymbolStatus
    {
        public SymbolName SymbolName { get; private set; }
        public SourceStatus[] SourceStatuses { get; set; }
        public bool Stored { get; set; }
        public ExceptionStatus Exception { get; set; }

        public SymbolStatus(SymbolName symbolName)
        {
            SymbolName = symbolName;
        }

        public bool Check(bool expectedStored)
        {
            return Stored == expectedStored && Exception == null
                && (SourceStatuses == null || SourceStatuses.All(s => s.Check(expectedStored)));
        }
    }

    internal class ImageStatus
    {
        public ImageName ImageName { get; private set; }
        public SymbolStatus SymbolStatus { get; set; }
        public ExceptionStatus Exception { get; set; }

        public ImageStatus(ImageName imageName)
        {
            ImageName = imageName;
        }

        public bool Check(bool expectedStored)
        {
            return /*Stored == expectedStored &&*/ Exception == null
                && (SymbolStatus == null || SymbolStatus.Check(expectedStored));
        }
    }

    internal class ImageStatusFile
    {
        public ImageStatusFile(string filePath, ImageStatus imageStatus)
        {
            FilePath = Path.ChangeExtension(filePath, ".smbsrc");
            ImageStatus = imageStatus;
        }

        public string FilePath { get; set; }
        public ImageStatus ImageStatus { get; set; }
    }

    public class PackageProcessor : IPackageProcessor
    {
        private readonly IPackageProcessorConfiguration configuration;
        private readonly IStorageService storage;
        private readonly ISchedulerService scheduler;
        private readonly INotifierService notifier;
        private readonly ISupportService support;
        private readonly IAddInfoBuilder addInfoBuilder;
        private readonly IPdbStoreManager pdbStoreManager;
        private readonly IFileCompressor fileCompressor;

        public PackageProcessor(
            IPackageProcessorConfiguration configuration,
            IStorageService storage,
            ISchedulerService scheduler,
            INotifierService notifier,
            ISupportService support,
            IAddInfoBuilder addInfoBuilder,
            IPdbStoreManager pdbStoreManager,
            IFileCompressor fileCompressor)
        {
            this.configuration = configuration;
            this.storage = storage;
            this.scheduler = scheduler;
            this.notifier = notifier;
            this.support = support;
            this.addInfoBuilder = addInfoBuilder;
            this.pdbStoreManager = pdbStoreManager;
            this.fileCompressor = fileCompressor;
        }

        public async Task Process(PackageMessage message)
        {
            var before = DateTime.Now;

            try
            {
                switch (message.PackageState)
                {
                    case PackageState.New:
                        await Queue(message);
                        break;
                    case PackageState.IndexingQueued:
                        await Index(message);
                        break;
                    case PackageState.DeletingQueued:
                        await Delete(message);
                        break;
                    case PackageState.Partial:
                        await Retry(message);
                        break;
                    case PackageState.Original:
                    case PackageState.Indexing:
                    case PackageState.Succeded:
                    case PackageState.Deleting:
                    case PackageState.Deleted:
                        throw new NotSupportedException();
                    default:
                        throw new ArgumentOutOfRangeException("packageState", message.PackageState, null);
                }

                support.TrackRequest(message.PackageState, before, DateTime.Now - before, true);
            }
            catch (Exception e)
            {
                support.TrackException(e, new { message });
                support.TrackRequest(message.PackageState, before, DateTime.Now - before, false);
                throw;
            }

        }

        private async Task<bool> ProcessPackage(UserInfo userInfo, string feedName, PackageName packageName, PackageState packageState, PackageState damagedPackageState, Func<IPackageStorageItem, Task<IPackageStorageItem>> tryWithPackageItemBefore, Func<IPackageStorageItem, ZipPackage, Task> tryWithPackage, Func<IPackageStorageItem, Task> tryWithPackageItemAfter)
        {
            var feed = storage.GetFeed(feedName);
            var packageItem = feed.GetPackage(null, packageState, packageName);
            var traceName = packageItem.ToString();

            try
            {
                packageItem = await tryWithPackageItemBefore(packageItem);

                if (packageItem == null)
                    throw new Exception(string.Format("Missing package {0}", traceName));

                using (var packageStream = await packageItem.Get())
                {
                    if (packageStream == null)
                        throw new Exception(string.Format("Missing package data {0}", traceName));

                    var package = new ZipPackage(packageStream);
                    await tryWithPackage(packageItem, package);
                }

                await tryWithPackageItemAfter(packageItem);
                return true;
            }
            catch (Exception e)
            {
                support.TrackException(e, new { packageName });
                Trace.TraceError("Unexpected error while processing package {0}:\n{1}", traceName, e);
                notifier.Damaged(userInfo, packageName).Wait();

                if (packageItem != null)
                    packageItem.Move(damagedPackageState, packageName).Wait();

                return false;
            }
        }

        private async Task Queue(PackageMessage message)
        {
            var task = new QueuePackageTask();
            PackageName newPackageName = null;

            // ReSharper disable RedundantArgumentName
            await ProcessPackage(
                message.UserInfo, message.FeedName, message.PackageName,
                PackageState.New, PackageState.DamagedNew,
                tryWithPackageItemBefore: async pi =>
                {
                    Trace.TraceInformation("Reading package {0}", pi);
                    return pi;
                },
                tryWithPackage: async (pi, p) =>
                {
                    newPackageName = await task.ReadName(message.UserInfo, p);
                    await notifier.Starting(message.UserInfo, newPackageName);
                },
                tryWithPackageItemAfter: async pi =>
                {
                    Trace.TraceInformation("Queueing package {0} as {1}", pi, newPackageName);

                    if (await pi.Copy(PackageState.Original, newPackageName) == null)
                        throw new Exception(string.Format("Failed to copy package {0} to {1}", pi, newPackageName));

                    if (await pi.Move(PackageState.IndexingQueued, newPackageName) == null)
                        throw new Exception(string.Format("Failed to move package {0} to {1}", pi, newPackageName));

                    message.PackageState = PackageState.IndexingQueued;
                    message.PackageName = newPackageName;
                    await scheduler.Signal(message);

                    Trace.TraceInformation("Finished queueing package {0} as {1}", pi, newPackageName);
                });
            // ReSharper restore RedundantArgumentName
        }

        private async Task Index(PackageMessage message)
        {
            var task = new IndexPackageTask(configuration, notifier, support, addInfoBuilder, pdbStoreManager, fileCompressor);

            // ReSharper disable RedundantArgumentName
            await ProcessPackage(
                message.UserInfo, message.FeedName, message.PackageName,
                PackageState.IndexingQueued, PackageState.DamagedIndexing,
                tryWithPackageItemBefore: async pi =>
                {
                    Trace.TraceInformation("Indexing package {0}", pi);
                    // ReSharper disable once ConvertToLambdaExpression
                    return await pi.Move(PackageState.Indexing, pi.Name);
                },
               tryWithPackage: async (pi, p) =>
               {
                   await task.IndexPackage(message.UserInfo, pi.Feed, pi.Name, pi, p);
               },
               tryWithPackageItemAfter: async pi =>
               {
                   await pi.Delete();
                   Trace.TraceInformation("Finished indexing package {0}", pi);
               });
            // ReSharper restore RedundantArgumentName
        }

        private async Task Delete(PackageMessage message)
        {
            var task = new DeletePackageTask(notifier, support);

            // ReSharper disable RedundantArgumentName
            await ProcessPackage(
                message.UserInfo, message.FeedName, message.PackageName,
                PackageState.DeletingQueued, PackageState.DamagedDeleting,
                tryWithPackageItemBefore: async pi =>
                {
                    Trace.TraceInformation("Deleting package {0}", pi);
                    // ReSharper disable once ConvertToLambdaExpression
                    return await pi.Move(PackageState.Deleting, pi.Name);
                },
                tryWithPackage: async (pi, p) =>
                {
                    await task.DeletePackage(message.UserInfo, pi.Feed, pi.Name, pi, p);
                }, tryWithPackageItemAfter: async pi =>
                {
                    await pi.Delete();
                    Trace.TraceInformation("Finished deleting package {0}", pi);
                });
            // ReSharper restore RedundantArgumentName
        }

        private async Task Retry(PackageMessage message)
        {
            var deleteTask = new DeletePackageTask(notifier, support);

            // ReSharper disable RedundantArgumentName
            if (!await ProcessPackage(
                message.UserInfo, message.FeedName, message.PackageName,
                PackageState.Partial, PackageState.DamagedDeleting,
                tryWithPackageItemBefore: async pi =>
                {
                    Trace.TraceInformation("Retrying package {0}", pi);
                    return await pi.Move(PackageState.Deleting, pi.Name);
                },
                tryWithPackage: async (pi, p) =>
                {
                    await deleteTask.DeletePackage(message.UserInfo, pi.Feed, pi.Name, pi, p);
                },
                tryWithPackageItemAfter: async pi =>
                {
                    await pi.Delete();
                }))
            // ReSharper restore RedundantArgumentName
            {
                return;
            }

            var indexTask = new IndexPackageTask(configuration, notifier, support, addInfoBuilder, pdbStoreManager, fileCompressor);

            // ReSharper disable RedundantArgumentName
            if (!await ProcessPackage(
                message.UserInfo, message.FeedName, message.PackageName,
                PackageState.Original, PackageState.DamagedIndexing,
                tryWithPackageItemBefore: async pi =>
                {
                    return await pi.Copy(PackageState.Indexing, pi.Name);
                },
                tryWithPackage: async (pi, p) =>
                {
                    await indexTask.IndexPackage(message.UserInfo, pi.Feed, pi.Name, pi, p);
                },
                tryWithPackageItemAfter: async pi =>
                {
                    Trace.TraceInformation("Finished retrying package {0}", pi);
                    await pi.Delete();
                }))
            // ReSharper restore RedundantArgumentName
            {
                return;
            }
        }
    }
}
