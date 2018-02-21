namespace SymbolSource.Contract.Storage
{
    public interface IAzureStorageConfiguration
    {
        string ConnectionString { get; set; }
    }

    public class AzureStorageConfiguration : IAzureStorageConfiguration
    {
        private readonly IConfigurationService configuration;

        public AzureStorageConfiguration(IConfigurationService configuration)
        {
            this.configuration = configuration;
        }

        public string ConnectionString
        {
            get { return configuration["AzureStorage.ConnectionString"]; }
            set { configuration["AzureStorage.ConnectionString"] = value; }
        }
    }
}