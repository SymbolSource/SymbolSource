using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NuGet;
using SymbolSource.Contract.Security;
using SymbolSource.Server.Tests.Clients;
using SymbolSource.Server.Tests.Servers;

namespace SymbolSource.Server.Tests
{
    public class NuGetOrgSecurityTests
    {
        private readonly Func<Uri, string, TestClient> ctor;

        protected NuGetOrgSecurityTests(Func<Uri, string, TestClient> ctor)
        {
            this.ctor = ctor;
        }

        public void Test()
        {
            var mock = new Mock<INuGetOrgSecurityEndpoint>(MockBehavior.Strict);

            using (var server = new NuGetOrgTestServer(null, 57739, mock.Object))
            using (var client = ctor(server.Uri, "test"))
            {

            }
        }
    }
}
