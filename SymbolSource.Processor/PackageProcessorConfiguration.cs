using System;
using SymbolSource.Contract;

namespace SymbolSource.Processor
{
    public interface IPackageProcessorConfiguration
    {
        string ServerUrl { get; }
    }

    public class PackageProcessorConfiguration : IPackageProcessorConfiguration
    {
        private readonly IConfigurationService configuration;

        public PackageProcessorConfiguration(IConfigurationService configuration)
        {
            this.configuration = configuration;
        }

        public string ServerUrl
        {
            get
            {
                var uri = new Uri(configuration["PackageProcessor.ServerUrl"]);

                if (uri.Scheme != "http" && uri.Scheme != "https")
                    // ReSharper disable once NotResolvedInText
                    throw new ArgumentOutOfRangeException("PackageProcessor.ServerUrl", uri, "Unsupported scheme");

                return uri.ToString().TrimEnd('/');
            }
        }
    }
}
