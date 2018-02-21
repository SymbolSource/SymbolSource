using System;
using System.IO;
using System.Linq;
using Xunit;
using System.Threading.Tasks;
using SymbolSource.Contract;
using SymbolSource.Server.Tests.Clients;
using SymbolSource.Server.Tests.Servers;

namespace SymbolSource.Server.Tests
{
    public abstract class NullServerTests : IDisposable
    {
        private string rootPath;
        private string apiKey;
        private readonly NullTestServer server;
        private readonly TestClient client;
        private readonly TestClient clientForNew;

        private static int port = 57000;

        protected NullServerTests(Func<Uri, string, TestClient> ctor)
        {
            rootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            apiKey = Guid.NewGuid().ToString();
            server = new NullTestServer(rootPath, ++port, apiKey);
            client = ctor(server.Uri, string.Empty);
            clientForNew = ctor(server.Uri, ",all,new");
        }

        public void Dispose()
        {
            server.Dispose();
            client.Dispose();
            Directory.Delete(rootPath, true);
        }

        [Fact]
        public async Task TestPushAndDelete()
        {
            var packageName = new PackageName("test", "1.0");

            using (var stream = new MemoryStream())
            {
                client.Generate(stream, packageName);
                stream.Position = 0;

                Assert.Empty(await client.Query());
                Assert.Empty(await clientForNew.Query());
                client.Push(stream, apiKey);
                Assert.Empty(await client.Query());
                var packageNames = await clientForNew.Query();
                Assert.Equal(1, packageNames.Count);

                clientForNew.Delete(apiKey, packageNames[0]);
                Assert.Empty(await clientForNew.Query());
            }
        }

        [Fact]
        public async Task TestPushWithInvalidKey()
        {
            var invalidApiKey = Guid.NewGuid().ToString();
            var packageName = new PackageName("test", "1.0");

            using (var stream = new MemoryStream())
            {
                client.Generate(stream, packageName);
                stream.Position = 0;

                Assert.Empty(await client.Query());
                Assert.Empty(await clientForNew.Query());
                var exceptionPush = Assert.Throws<InvalidOperationException>(() => client.Push(stream, invalidApiKey));
                Assert.Contains("403", exceptionPush.Message);
                Assert.Empty(await client.Query());
                Assert.Empty(await clientForNew.Query());

                var exceptionDelete = Assert.Throws<InvalidOperationException>(() => clientForNew.Delete(apiKey, packageName));
                Assert.Contains("404", exceptionDelete.Message);
            }
        }

        [Fact]
        public async Task TestPushAndDeleteWithInvalidKey()
        {
            var invalidApiKey = Guid.NewGuid().ToString();
            var packageName = new PackageName("test", "1.0");

            using (var stream = new MemoryStream())
            {
                client.Generate(stream, packageName);
                stream.Position = 0;

                Assert.Empty(await client.Query());
                Assert.Empty(await clientForNew.Query());
                client.Push(stream, apiKey);

                var packageNames = await clientForNew.Query();
                Assert.Equal(1, packageNames.Count);
                var exception = Assert.Throws<InvalidOperationException>(() => client.Delete(invalidApiKey, packageNames[0]));
                Assert.Contains("403", exception.Message);
                Assert.NotEmpty(await clientForNew.Query());
            }
        }
    }

    public class Protocol2DefaultFeedNullServerTests : NullServerTests
    {
        public Protocol2DefaultFeedNullServerTests()
            : base((uri, subfeed) => new Protocol2TestClient(uri, subfeed))
        {
        }
    }

    public class Protocol2NamedFeedNullServerTests : NullServerTests
    {
        public Protocol2NamedFeedNullServerTests()
            : base((uri, subfeed) => new Protocol2TestClient(uri, "test" + subfeed))
        {
        }
    }

    public class Protocol3DefaultFeedNullServerTests : NullServerTests
    {
        public Protocol3DefaultFeedNullServerTests()
            : base((uri, subfeed) => new Protocol3TestClient(uri, subfeed))
        {
        }
    }

    public class Protocol3NamedFeedNullServerTests : NullServerTests
    {
        public Protocol3NamedFeedNullServerTests()
            : base((uri, subfeed) => new Protocol3TestClient(uri, "test" + subfeed))
        {
        }
    }
}
