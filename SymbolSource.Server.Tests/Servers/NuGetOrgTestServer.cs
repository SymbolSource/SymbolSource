using System.IO;
using Autofac;
using SymbolSource.Contract;
using SymbolSource.Contract.Container;
using SymbolSource.Contract.Scheduler;
using SymbolSource.Contract.Security;
using SymbolSource.Contract.Storage;

namespace SymbolSource.Server.Tests.Servers
{
    internal class NuGetOrgTestServer : TestServer
    {
        private readonly INuGetOrgSecurityEndpoint endpoint;

        public NuGetOrgTestServer(string rootPath, int port, INuGetOrgSecurityEndpoint endpoint)
            : base(rootPath, port, Configure, builder => Register(builder, endpoint))
        {
            this.endpoint = endpoint;
        }

        private static void Configure(IConfigurationService configuration)
        {
            // ReSharper disable once ObjectCreationAsStatement
            new ContainerConfiguration(configuration)
            {
                SecurityType = typeof(NuGetOrgSecurityService),
            };
        }

        private static void Register(ContainerBuilder builder, INuGetOrgSecurityEndpoint endpoint)
        {
            builder.RegisterInstance(endpoint).As<INuGetOrgSecurityEndpoint>();
        }
    }
}