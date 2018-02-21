using System;
using System.IO;
using System.Web.Http;
using Autofac;
using Microsoft.Owin.Hosting;
using Owin;
using SymbolSource.Contract;
using SymbolSource.Contract.Container;
using SymbolSource.Contract.Scheduler;
using SymbolSource.Contract.Storage;
using SymbolSource.Support;

namespace SymbolSource.Server.Tests.Servers
{
    public abstract class TestServer : IDisposable
    {
        private readonly Uri uri;
        private readonly IDisposable webApp;

        protected TestServer(string rootPath, int port, Action<IConfigurationService> configure, Action<ContainerBuilder> register)
        {
            var uriBuilder = new UriBuilder
            {
                Host = "localhost",
                Port = port
            };

            uri = uriBuilder.Uri;

            webApp = WebApp.Start(uri.ToString(), builder =>
                {
                    var httpConfiguration = new HttpConfiguration();
                    var configuration = new MemoryConfigurationService();

                    new ContainerConfiguration(configuration)
                    {
                        StorageType = typeof(LocalStorageService),
                        SchedulerType = typeof(FileSchedulerService)
                    };

                    // ReSharper disable once ObjectCreationAsStatement
                    new InstanceConfiguration(configuration)
                    {
                        InstanceName = "test",
                        InstanceSalt = "test"
                    };

                    // ReSharper disable once ObjectCreationAsStatement
                    new SupportConfiguration(SupportEnvironment.WebApp, configuration, null)
                    {
                        IntercomAppId = null,
                        IntercomApiKey = null,
                        InsightsWebAppInstrumentationKey = null,
                        InsightsWebJobInstrumentationKey = null
                    };

                    // ReSharper disable once ObjectCreationAsStatement
                    new LocalStorageConfiguration(configuration)
                    {
                        RootPath = rootPath
                    };

                    configure(configuration);

                    WebApiConfiguration.Register(httpConfiguration, configuration, register);
                    builder.UseWebApi(httpConfiguration);
                });
        }

        public void Dispose()
        {
            webApp.Dispose();
        }

        public Uri Uri
        {
            get { return uri; }
        }
    }
}