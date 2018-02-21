using System;
using Moq;
using SymbolSource.Contract.Security;
using SymbolSource.Server.Tests.Clients;
using SymbolSource.Server.Tests.Servers;

namespace SymbolSource.Server.Tests
{
    public abstract class NuGetOrgNamedFeedTests : IDisposable
    {
        private readonly TestClient client;
        private readonly NuGetOrgTestServer server;

        protected NuGetOrgNamedFeedTests(Func<Uri, TestClient> ctor)
        {
            var mock = new Mock<INuGetOrgSecurityEndpoint>(MockBehavior.Strict);
            server = new NuGetOrgTestServer(null, 57739, mock.Object);
            client = ctor(server.Uri);
        }

        public void Dispose()
        {
            server.Dispose();
            client.Dispose();
        }

        //fail all
    }

    public class Protocol2NuGetOrgNamedFeedTests : NuGetOrgNamedFeedTests
    {
        public Protocol2NuGetOrgNamedFeedTests()
            :base(uri => new Protocol2TestClient(uri, "test"))
        {
        }
    }

    public class Protocol3NuGetOrgNamedFeedTests : NuGetOrgNamedFeedTests
    {
        public Protocol3NuGetOrgNamedFeedTests()
            : base(uri => new Protocol3TestClient(uri, "test"))
        {
        }
    }
}