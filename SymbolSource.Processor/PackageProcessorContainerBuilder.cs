using Autofac;
using SymbolSource.Contract.Processor;
using SymbolSource.Processor.Legacy;
using SymbolSource.Processor.Notifier;

namespace SymbolSource.Processor
{
    public static class PackageProcessorContainerBuilder
    {
        public static void Register(ContainerBuilder builder)
        {
            builder.RegisterType<PackageProcessorConfiguration>()
                .As<IPackageProcessorConfiguration>();

            builder.RegisterType<PackageProcessor>()
                .As<IPackageProcessor>();

            builder.RegisterType<NotifierService>()
                .As<INotifierService>();

            builder.RegisterType<SupportNotifierEndpoint>()
               .As<INotifierEndpoint>();

            builder.RegisterType<TwitterNotifierEndpoint>()
                .As<INotifierEndpoint>();

            builder.RegisterType<EmailNotifierEndpoint>()
                .As<INotifierEndpoint>();

            builder.RegisterModule<ProcessingBasicInstaller>();
        }
    }
}
