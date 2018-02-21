using System.Collections.Generic;
using System.IO;
using System.Linq;
using SymbolSource.Contract.Storage;
using SymbolSource.Contract.Support;

namespace SymbolSource.Contract.Tests
{
    internal class AzureStorageTestConfiguration : IAzureStorageConfiguration
    {
        public string ConnectionString { get; set; }
    }

    public class StorageTestService : IStorageService
    {
        private readonly IStorageService storage;
        private readonly string feedSuffix;

        public StorageTestService(IStorageService storage, string feedSuffix)
        {
            this.storage = storage;
            this.feedSuffix = feedSuffix;
        }

        public IStorageFeed GetFeed(string feedName)
        {
            return storage.GetFeed(feedName + feedSuffix);
        }

        public IEnumerable<string> QueryFeeds()
        {
            return storage.QueryFeeds()
                .Where(feedName => feedName.EndsWith(feedSuffix))
                .Select(feedName => feedName.Substring(0, feedName.Length - feedSuffix.Length));
        }
    }

    public class AzureStorageServiceTests : StorageServiceTests
    {
        private readonly IStorageService storage;

        public AzureStorageServiceTests()
        {
            storage = new StorageTestService(
                new AzureStorageService(
                    new AzureStorageTestConfiguration
                    {
                        ConnectionString = 
                            "DefaultEndpointsProtocol=https;"
                            + "AccountName=smbsrctest;"
                            + "AccountKey=q8KVk0QG+iIIsjKiIZ6cnBcGuYyXRwHgHgIcA1TUMegVCKwJE5tmC8XWH4XCvDnCVm2Xua6Vro8SRi/Sk/IyxQ=="
                    },
                    new NullSupportService()),
                Path.GetRandomFileName().Replace(".", ""));
        }

        protected override IStorageService Storage
        {
            get { return storage; }
        }
    }
}
