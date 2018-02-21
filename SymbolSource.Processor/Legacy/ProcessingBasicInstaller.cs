using Autofac;
using SymbolSource.Processor.Legacy.Projects;

namespace SymbolSource.Processor.Legacy
{
    public class ProcessingBasicInstaller : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BinaryStoreManager>().As<IBinaryStoreManager>();
            builder.RegisterType<SourceDiscover>().As<SourceDiscover>();
            builder.RegisterType<AddInfoBuilder>().As<IAddInfoBuilder>();
            builder.RegisterType<SourceStoreManager>().As<ISourceStoreManager>();
            builder.RegisterType<SymbolStoreManager>().As<ISymbolStoreManager>();
            builder.RegisterType<SrcToolSourceExtractor>().As<ISourceExtractor>();
            builder.RegisterType<PdbStoreManager>().As<IPdbStoreManager>();
            builder.RegisterType<FileCompressor>().As<IFileCompressor>();

            //builder.RegisterInstance(new DictionaryAdapterFactory().GetAdapter<IPdbStoreConfig>(ConfigurationManager.AppSettings)).As<IPdbStoreConfig>()
        }
    }
}