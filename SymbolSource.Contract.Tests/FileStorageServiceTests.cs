using System.IO;
using SymbolSource.Contract.Storage;

namespace SymbolSource.Contract.Tests
{
    internal class LocalStorageTestConfiguration : ILocalStorageConfiguration
    {
        public string RootPath { get; set; }
    }

    public class FileStorageServiceTests : StorageServiceTests
    {
        private readonly LocalStorageService storage;

        public FileStorageServiceTests()
        {
            var configuration = new LocalStorageTestConfiguration
            {
                RootPath = Path.GetTempFileName()
            };

            storage = new LocalStorageService(configuration);
            File.Delete(configuration.RootPath);
            Directory.CreateDirectory(configuration.RootPath);
        }

        protected override IStorageService Storage
        {
            get { return storage; }
        }
    }
}
