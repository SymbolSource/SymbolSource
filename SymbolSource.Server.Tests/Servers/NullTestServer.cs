using System.IO;
using Autofac;
using SymbolSource.Contract;
using SymbolSource.Contract.Container;
using SymbolSource.Contract.Scheduler;
using SymbolSource.Contract.Security;
using SymbolSource.Contract.Storage;

namespace SymbolSource.Server.Tests.Servers
{
    internal class NullTestServer : TestServer
    {
        public NullTestServer(string rootPath, int port, string apiKey)
            : base(rootPath, port, configuration => Configure(configuration, apiKey), null)
        {
        }

        private static void Configure(IConfigurationService configuration, string apiKey)
        {
            // ReSharper disable once ObjectCreationAsStatement
            new ContainerConfiguration(configuration)
            {
                SecurityType = typeof(NullSecurityService),
            };

            // ReSharper disable once ObjectCreationAsStatement
            new NullSecurityConfiguration(configuration, null)
            {
                AllowNamedFeeds = true,
                PushApiKeys = new[] { apiKey }
            };
        }
    }
}