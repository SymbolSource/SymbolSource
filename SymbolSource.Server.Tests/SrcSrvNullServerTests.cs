using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using NuGet;
using SymbolSource.Server.Tests.Clients;
using SymbolSource.Server.Tests.Servers;
using Xunit;

namespace SymbolSource.Server.Tests
{
    public abstract class SrcSrvNullServerTests : IDisposable
    {
        private readonly NullTestServer server;
        private readonly SrcSrvTestClient client;

        protected SrcSrvNullServerTests(string feed)
        {
            server = new NullTestServer(null, 57739, null);
            client = new SrcSrvTestClient(server.Uri, feed);
        }

        public void Dispose()
        {
            server.Dispose();
            client.Dispose();
        }

        [Fact]
        public void Symbol()
        {
            client.Symbol();
        }
    }

    public class SrcSrvDefaultFeedTests : SrcSrvNullServerTests
    {
        public SrcSrvDefaultFeedTests()
            : base(null)
        {
        }
    }

    public class SrcSrvNamedFeedTests : SrcSrvNullServerTests
    {
        public SrcSrvNamedFeedTests()
            : base("test")
        {
        }
    }
}
