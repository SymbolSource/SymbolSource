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
    public class MyGetOrgSecurityTests
    {
        private readonly Func<Uri, string, TestClient> ctor;

        protected MyGetOrgSecurityTests(Func<Uri, string, TestClient> ctor)
        {
            this.ctor = ctor;
        }

        public void Test()
        {
            var mock = new Mock<IMyGetOrgSecurityEndpoint>(MockBehavior.Strict);

            using (var server = new MyGetOrgTestServer(null, 57739, mock.Object))
            using (var client = ctor(server.Uri, "test"))
            {

            }
        }
    }
}
