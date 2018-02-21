using System.Configuration;
using SymbolSource.Contract.Storage;

namespace SymbolSource.Contract.Scheduler
{
    public interface IWebJobsSchedulerConfiguration
    {
        string StorageConnectionString { get; }
        string DashboardConnectionString { get; }
        int BatchSize { get; }
    }

    public class WebJobsSchedulerConfiguration : IWebJobsSchedulerConfiguration
    {
        private readonly IConfigurationService configuration;
        private readonly IAzureStorageConfiguration azureConfiguration;

        public WebJobsSchedulerConfiguration(
            IConfigurationService configuration, 
            IAzureStorageConfiguration azureConfiguration)
        {
            this.configuration = configuration;
            this.azureConfiguration = azureConfiguration;
        }

        public string StorageConnectionString
        {
            get { return azureConfiguration.ConnectionString; }
        }

        public string DashboardConnectionString
        {
            get { return azureConfiguration.ConnectionString; }
        }

        public int BatchSize
        {
            get { return int.Parse(configuration["PackageProcessor.BatchSize"]); }
        }
    }
}