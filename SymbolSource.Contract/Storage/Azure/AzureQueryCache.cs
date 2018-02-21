using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SymbolSource.Contract.Support;

namespace SymbolSource.Contract.Storage.Azure
{
    internal class AzureQueryCache<T>
    {
        private readonly ISupportService support;
        private readonly ConcurrentDictionary<string, AzureQueryCacheEntry<T>> entries;

        public AzureQueryCache(ISupportService support)
        {
            this.support = support;
            entries = new ConcurrentDictionary<string, AzureQueryCacheEntry<T>>();
        }

        public async Task<IEnumerable<T>> Get(string key, int skip, int take, TimeSpan timeout, Func<object, int, Task<ResultSegment<T, object>>> load)
        {
            var entry = entries.GetOrAdd(key, k =>
            {
                return new AzureQueryCacheEntry<T>(
                    load, async e =>
                    {
                        while (DateTime.Now - e.LastAccess < timeout)
                            await Task.Delay(timeout);

                        entries.TryRemove(k, out e);
                    });
            });

            support.TrackMetric(this, entries.Count, null);
            return await entry.Get(skip, take);
        }
    }

    internal class AzureQueryCacheEntry<T>
    {
        private readonly Func<object, int, Task<ResultSegment<T, object>>> load;
        private readonly Func<AzureQueryCacheEntry<T>, Task> remove;
        private readonly SemaphoreSlim semaphore;
        private readonly IList<AzureQueryCacheSegment<T>> segments;
        private int sum;
        private Task removeTask;
        private DateTime lastAccess;

        public AzureQueryCacheEntry(
            Func<object, int, Task<ResultSegment<T, object>>> load,
            Func<AzureQueryCacheEntry<T>, Task> remove)
        {
            this.load = load;
            this.remove = remove;
            semaphore = new SemaphoreSlim(1);
            segments = new List<AzureQueryCacheSegment<T>>();
        }

        public DateTime LastAccess
        {
            get { return lastAccess; }
        }

        public async Task<IEnumerable<T>> Get(int skip, int take)
        {
            Task<ResultSegment<T, object>> lastTask;
            await semaphore.WaitAsync();

            try
            {
                lastAccess = DateTime.Now;

                if (removeTask == null)
                    removeTask = remove(this);

                if (skip == 0)
                {
                    segments.Clear();
                    sum = 0;
                }

                if (segments.Count == 0)
                {
                    lastTask = load(null, take);
                    sum += take;
                    segments.Add(new AzureQueryCacheSegment<T>(take, lastTask));
                }
                else
                {
                    lastTask = segments[segments.Count - 1].Task;
                }

                while (sum < skip + take)
                {
                    var lastSegment = await lastTask;

                    if (lastSegment.ContinuationToken == null)
                        break;

                    lastTask = load(lastSegment.ContinuationToken, take);
                    sum += take;
                    segments.Add(new AzureQueryCacheSegment<T>(take, lastTask));
                }
            }
            finally
            {
                semaphore.Release();
            }

            await lastTask;

            return segments.SelectMany(t => t.Task.Result.Results).Skip(skip).Take(take);
        }
    }

    internal class AzureQueryCacheSegment<T>
    {
        public AzureQueryCacheSegment(int count, Task<ResultSegment<T, object>> task)
        {
            Count = count;
            Task = task;
        }

        public int Count { get; private set; }
        public Task<ResultSegment<T, object>> Task { get; private set; }
    }
}