using System.Diagnostics;
using Autofac;
using SymbolSource.Contract;
using SymbolSource.Contract.Container;
using SymbolSource.Support;
using Topshelf;
using Topshelf.Autofac;

namespace SymbolSource.Processor.Topshelf
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            Trace.Listeners.Add(new TextWriterTraceListener("log.txt"));

            foreach (var assembly in typeof(PackageProcessor).Assembly.GetReferencedAssemblies())
                Trace.WriteLine(assembly.FullName);

            var container = Container();

            HostFactory.Run(c =>
            {
                c.UseAutofacContainer(container);

                c.Service<ProcessorService>(s =>
                {
                    s.ConstructUsingAutofacContainer();
                    s.WhenStarted((service, control) => service.Start());
                    s.WhenStopped((service, control) => service.Stop());
                });
            });
        }

        private static IContainer Container()
        {
            var configuration = new DefaultConfigurationService();
            var builder = new ContainerBuilder();

            DefaultContainerBuilder.Register(builder, configuration);
            SupportContainerBuilder.Register(builder, SupportEnvironment.WebJob);
            PackageProcessorContainerBuilder.Register(builder);

            builder.RegisterType<ProcessorService>();
            
            var container = builder.Build();
            return container;
        }
    }
}