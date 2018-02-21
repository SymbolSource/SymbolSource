using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.Queryable;

namespace SymbolSource.Contract.Storage.Azure
{
    internal static class KeyValuePairExtensions
    {
        public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }
    }

    internal class AzureTable<T>
        where T : TableEntity, new()
    {
        private readonly CloudTable table;

        public AzureTable(CloudTable table)
        {
            this.table = table;
        }

        public async Task<bool> CreateIfNotExistsAsync()
        {
            return await table.CreateIfNotExistsAsync();
        }

        protected IEnumerable<T> Query(string partitionKey)
        {
            var query = table.CreateQuery<T>()
                .Where(e => e.PartitionKey == partitionKey)
                .AsTableQuery();

            IEnumerator<T> enumerator;

            try
            {
                enumerator = query.Execute().GetEnumerator();

                if (!enumerator.MoveNext())
                    yield break;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 404)
                    yield break;

                throw;
            }

            yield return enumerator.Current;

            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        protected async Task<AzureTableResultSegment<T>> QuerySegmented(string partitionKey, string rowKeyPrefix, TableContinuationToken continuationToken, int take)
        {
            var query = table.CreateQuery<T>().Where(e => e.PartitionKey == partitionKey);

            if (!string.IsNullOrEmpty(rowKeyPrefix))
                query = query.WhereRowKeyStartsWith(rowKeyPrefix);

            var tableQuery = query.Take(take).AsTableQuery();

            try
            {
                var segment = await tableQuery.ExecuteSegmentedAsync(continuationToken);
                return new AzureTableResultSegment<T>(segment.Results, segment.ContinuationToken);
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 404)
                    return new AzureTableResultSegment<T>(new T[0], null);

                throw;
            }
        }

        protected async Task<T> RetrieveAsync(string partitionKey, string rowKey)
        {
            var operation = TableOperation.Retrieve<T>(partitionKey, rowKey);

            var result = await table.ExecuteAsync(operation).ConfigureAwait(false);

            if (result.HttpStatusCode == 404)
                return null;

            return (T)result.Result;
        }

        protected async Task InsertOrReplaceAsync(string partitionKey, string rowKey, T entity)
        {
            entity.PartitionKey = partitionKey;
            entity.RowKey = rowKey;
            var operation = TableOperation.InsertOrReplace(entity);
            await table.ExecuteAsync(operation);
        }

        protected async Task<bool> DeleteIfExistsAsync(string partitionKey, string rowKey)
        {
            var entity = new TableEntity(partitionKey, rowKey) { ETag = "*" };

            try
            {
                var operation = TableOperation.Delete(entity);
                await table.ExecuteAsync(operation);
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 404)
                    return false;

                throw;
            }

            return true;
        }
    }

    public static class AzureTableExtensions
    {
        public static IQueryable<T> WherePartitionKeyStartsWith<T>(this IQueryable<T> query, string value) 
            where T : ITableEntity
        {
            var upperBoundValue = CreateUpperBoundString(value);
            return query.Where(x => x.PartitionKey.CompareTo(value) >= 0 && x.PartitionKey.CompareTo(upperBoundValue) < 0);
        }

        public static IQueryable<T> WhereRowKeyStartsWith<T>(this IQueryable<T> query, string value)
            where T : ITableEntity
        {
            var upperBoundValue = CreateUpperBoundString(value);
            return query.Where(x => x.RowKey.CompareTo(value) >= 0 && x.RowKey.CompareTo(upperBoundValue) < 0);
        }

        private static string CreateUpperBoundString(string lowerBoundString)
        {
            var upperBoundSubstring = lowerBoundString.Substring(0, lowerBoundString.Length - 1);
            var upperBoundChar = (char)(lowerBoundString[lowerBoundString.Length - 1] + 1);
            return upperBoundSubstring + upperBoundChar;
        }

    }

    internal class AzureTableResultSegment<T> : ResultSegment<T, TableContinuationToken>
    {
        public AzureTableResultSegment(IEnumerable<T> results, TableContinuationToken continuationToken)
            : base(results, continuationToken)
        {
        }
    }

    internal class PackageTableEntity : TableEntity
    {
        public string UserName { get; set; }
    }

    internal class PackageTable : AzureTable<PackageTableEntity>
    {
        public PackageTable(CloudTable table)
            : base(table)
        {
        }

        public static string GetPartitionKey(PackageState packageState)
        {
            return string.Format("pkg*{0}", packageState.ToString().ToLower());
        }

        public static string GetRowKey(PackageName packageName)
        {
            return string.Format("{0}*{1}", packageName.Id, packageName.Version);
        }

        public static PackageName GetPackageName(string rowKey)
        {
            var rowKeyParts = rowKey.Split('*');
            return new PackageName(rowKeyParts[0], rowKeyParts[1]);
        }

        public async Task<AzureTableResultSegment<PackageEntry>> QuerySegmented(PackageState packageState, string packageNamePrefix, TableContinuationToken continuationToken, int take)
        {
            var segment = await QuerySegmented(GetPartitionKey(packageState), packageNamePrefix, continuationToken, take);

            return new AzureTableResultSegment<PackageEntry>(
                    segment.Results.Select(e => new PackageEntry(GetPackageName(e.RowKey), e)),
                    segment.ContinuationToken);
        }

        public Task<PackageTableEntity> RetrieveAsync(PackageState packageState, PackageName packageName)
        {
            return RetrieveAsync(GetPartitionKey(packageState), GetRowKey(packageName));
        }

        public Task InsertOrReplaceAsync(PackageState packageState, PackageName packageName, PackageTableEntity packageEntity)
        {
            return InsertOrReplaceAsync(GetPartitionKey(packageState), GetRowKey(packageName), packageEntity);
        }

        public Task<bool> DeleteIfExistsAsync(PackageState packageState, PackageName packageName)
        {
            return DeleteIfExistsAsync(GetPartitionKey(packageState), GetRowKey(packageName));
        }
    }

    internal class PackageEntry
    {
        public PackageEntry(PackageName name, PackageTableEntity entity)
        {
            Name = name;
            Entity = entity;
        }

        public PackageName Name { get; private set; }
        public PackageTableEntity Entity { get; private set; }
    }

    internal class RelatedPackageTableEntity : TableEntity
    {
    }

    internal class RelatedPackageTable : AzureTable<RelatedPackageTableEntity>
    {
        private readonly string partitionKey;

        public RelatedPackageTable(CloudTable table, string partitionKey)
            : base(table)
        {
            this.partitionKey = partitionKey;
        }

        public static string GetRowKey(PackageName packageName)
        {
            return string.Format("{0}*{1}", packageName.Id, packageName.Version);
        }

        internal static PackageName GetPackageName(string rowKey)
        {
            var rowKeyParts = rowKey.Split('*');
            return new PackageName(rowKeyParts[0], rowKeyParts[1]);
        }

        public IEnumerable<PackageName> Query()
        {
            return Query(partitionKey).Select(e => GetPackageName(e.RowKey));
        }

        public Task InsertOrReplaceAsync(PackageName packageName)
        {
            var entity = new RelatedPackageTableEntity();
            return InsertOrReplaceAsync(partitionKey, GetRowKey(packageName), entity);
        }

        public Task<bool> DeleteIfExistsAsync(PackageName packageName)
        {
            return DeleteIfExistsAsync(partitionKey, GetRowKey(packageName));
        }
    }

    internal class SymbolRelatedPackageTable : RelatedPackageTable
    {
        public SymbolRelatedPackageTable(CloudTable table, SymbolName symbolName)
            : base(table, GetPartitionKey(symbolName))
        {
        }

        private static string GetPartitionKey(SymbolName symbolName)
        {
            return string.Format("pdb*{0}*{1}", symbolName.ImageName, symbolName.SymbolHash);
        }
    }

    internal class SourceRelatedPackageTable : RelatedPackageTable
    {
        public SourceRelatedPackageTable(CloudTable table, SourceName sourceName)
            : base(table, GetPartitionKey(sourceName))
        {
        }

        private static string GetPartitionKey(SourceName sourceName)
        {
            return string.Format("src*{0}*{1}", sourceName.FileName, sourceName.Hash);
        }
    }
}