using System;
using Autofac;
using SymbolSource.Contract.Scheduler;
using SymbolSource.Contract.Security;
using SymbolSource.Contract.Storage;

namespace SymbolSource.Contract.Container
{
    public class DefaultContainerBuilder
    {
        public static void Register(ContainerBuilder builder, IConfigurationService configuration)
        {
            Register(builder, configuration, new ContainerConfiguration(configuration));
        }

        public static void Register(ContainerBuilder builder, IConfigurationService configuration, IContainerConfiguration containerConfiguration)
        {
            builder.RegisterInstance(configuration)
                .As<IConfigurationService>();

            builder.RegisterType<InstanceConfiguration>()
                .As<IInstanceConfiguration>();

            builder.RegisterType<NullSecurityConfiguration>()
                .As<INullSecurityConfiguration>();

            //builder.RegisterType<NuGetOrgSecurityConfiguration>()
            //    .As<INuGetOrgSecurityConfiguration>();

            builder.RegisterType<NuGetOrgSecurityEndpoint>()
                .As<INuGetOrgSecurityEndpoint>();

            builder.RegisterType<MyGetOrgSecurityConfiguration>()
               .As<IMyGetOrgSecurityConfiguration>();

            builder.RegisterType<MyGetOrgSecurityEndpoint>()
                .As<IMyGetOrgSecurityEndpoint>();

            builder.RegisterType<AzureStorageConfiguration>()
               .As<IAzureStorageConfiguration>();

            builder.RegisterType<LocalStorageConfiguration>()
                .As<ILocalStorageConfiguration>();

            builder.RegisterType<WebJobsSchedulerConfiguration>()
                .As<IWebJobsSchedulerConfiguration>();

            builder.RegisterType(containerConfiguration.SecurityType)
                .As<ISecurityService>()
                .SingleInstance();

            builder.RegisterType(containerConfiguration.StorageType)
                .As<IStorageService>()
                .SingleInstance();

            builder.RegisterType(containerConfiguration.SchedulerType)
                .As<ISchedulerService>()
                .SingleInstance();
        }
    }
}