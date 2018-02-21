using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NuGet;
using PackageName = SymbolSource.Contract.PackageName;

namespace SymbolSource.Processor.Processor
{
    public class PackageTask
    {
        private readonly ConcurrentDictionary<string, TimeSpan> requests;

        public PackageTask()
        {
            requests = new ConcurrentDictionary<string, TimeSpan>();
        }

        protected const int MaxSemanticVersionLength = 20;

        protected static PackageBuilder CreatePackage(PackageName packageName, IPackageMetadata package)
        {
            var packageBuilder = new PackageBuilder();
            packageBuilder.Id = packageName.Id;

            if (package != null)
            {
                packageBuilder.Populate(Manifest.Create(package).Metadata);

                if (packageBuilder.DependencySets != null)
                    packageBuilder.DependencySets.Clear();

                if (packageBuilder.PackageAssemblyReferences != null)
                    packageBuilder.PackageAssemblyReferences.Clear();
            }

            packageBuilder.Version = new SemanticVersion(
                packageName.Version.SubstringSafe(MaxSemanticVersionLength));

            return packageBuilder;
        }

        private static PhysicalPackageAssemblyReference CreateFile(string name, string content)
        {
            var buffer = Encoding.UTF8.GetBytes(content);
            var file = new PhysicalPackageAssemblyReference(() => new MemoryStream(buffer));
            file.SourcePath = name;
            file.TargetPath = name;
            return file;
        }

        protected static IPackageFile CreateFile<T>(string name, T content)
        {
            return CreateFile(name, JsonConvert.SerializeObject(content, Formatting.Indented));
        }

        protected static T ReadFile<T>(IPackageFile file)
        {
            using (var stream = file.GetStream())
            using (var reader = new StreamReader(stream))
                return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
        }

        protected async Task<TOutput[]> ProcessThrottled<TInput, TOutput>(int count, IEnumerable<TInput> items, Func<TInput, Task<TOutput>> task)
        {
            using (var semaphore = new SemaphoreSlim(count))
            {
                var tasks = items.Select(async item =>
                {
                    await semaphore.WaitAsync();

                    try
                    {
                        return await task(item);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                return await Task.WhenAll(tasks.ToArray());
            }
        }

        private async Task<TimeSpan> TimeExecution(Func<Task> action)
        {
            var before = DateTime.Now;
            await action();
            return DateTime.Now - before;
        }

        protected async Task<bool> RequestOrSkip(string key, Func<Task> action)
        {
            var existed = true;

            requests.GetOrAdd(key, k =>
            {
                existed = false;
                return TimeSpan.Zero;
            });

            if (!existed)
            {
                requests[key] = await TimeExecution(action);
            };

            return existed;
        }
    }
}