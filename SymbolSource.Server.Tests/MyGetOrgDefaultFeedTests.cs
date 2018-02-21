using System;
using Moq;
using SymbolSource.Contract.Security;
using SymbolSource.Server.Tests.Clients;
using SymbolSource.Server.Tests.Servers;

namespace SymbolSource.Server.Tests
{
    public abstract class MyGetOrgDefaultFeedTests : IDisposable
    {
        private readonly TestClient client;
        private readonly MyGetOrgTestServer server;

        protected MyGetOrgDefaultFeedTests(Func<Uri, TestClient> ctor)
        {
            var mock = new Mock<IMyGetOrgSecurityEndpoint>(MockBehavior.Strict);
            server = new MyGetOrgTestServer(null, 57739, mock.Object);
            client = ctor(server.Uri);
        }

        public void Dispose()
        {
            server.Dispose();
            client.Dispose();
        }

        //fail all
    }

    public class Protocol2MyGetOrgDefaultFeedTests : MyGetOrgDefaultFeedTests
    {
        public Protocol2MyGetOrgDefaultFeedTests()
            : base(uri => new Protocol2TestClient(uri, "test"))
        {
        }
    }

    public class Protocol3MyGetOrgDefaultFeedTests : MyGetOrgDefaultFeedTests
    {
        public Protocol3MyGetOrgDefaultFeedTests()
            : base(uri => new Protocol3TestClient(uri, "test"))
        {
        }
    }
}