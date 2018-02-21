namespace SymbolSource.Contract
{
    public interface IInstanceConfiguration
    {
        string InstanceName { get; set; }
        string InstanceSalt { get; set; }
    }

    public class InstanceConfiguration : IInstanceConfiguration
    {
        private readonly IConfigurationService configuration;

        public InstanceConfiguration(IConfigurationService configuration)
        {
            this.configuration = configuration;
        }

        public string InstanceName
        {
            get { return configuration["InstanceName"]; }
            set { configuration["InstanceName"] = value; }
        }

        public string InstanceSalt
        {
            get { return configuration["InstanceSalt"]; }
            set { configuration["InstanceSalt"] = value; }
        }
    }
}