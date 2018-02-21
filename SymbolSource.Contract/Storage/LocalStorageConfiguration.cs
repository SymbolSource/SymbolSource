namespace SymbolSource.Contract.Storage
{
    public interface ILocalStorageConfiguration
    {
        string RootPath { get; set; }
    }

    public class LocalStorageConfiguration : ILocalStorageConfiguration
    {
        private readonly IConfigurationService configuration;

        public LocalStorageConfiguration(IConfigurationService configuration)
        {
            this.configuration = configuration;
        }

        public string RootPath
        {
            get { return configuration["FileStorage.RootPath"]; }
            set { configuration["FileStorage.RootPath"] = value; }
        }
    }
}