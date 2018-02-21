using System.IO;
using Autofac;
using SymbolSource.Contract;
using SymbolSource.Contract.Container;
using SymbolSource.Contract.Scheduler;
using SymbolSource.Contract.Security;
using SymbolSource.Contract.Storage;

namespace SymbolSource.Server.Tests.Servers
{
    internal class MyGetOrgTestServer : TestServer
    {
        private readonly IMyGetOrgSecurityEndpoint endpoint;

        public MyGetOrgTestServer(string rootPath, int port, IMyGetOrgSecurityEndpoint endpoint)
            : base(rootPath, port, Configure, builder => Register(builder, endpoint))
        {
            this.endpoint = endpoint;
        }

        private static void Configure(IConfigurationService configuration)
        {
            // ReSharper disable once ObjectCreationAsStatement
            new ContainerConfiguration(configuration)
            {
                SecurityType = typeof(MyGetOrgSecurityService),
            };
        }

        private static void Register(ContainerBuilder builder, IMyGetOrgSecurityEndpoint endpoint)
        {
            builder.RegisterInstance(endpoint).As<IMyGetOrgSecurityEndpoint>();
        }
    }
}