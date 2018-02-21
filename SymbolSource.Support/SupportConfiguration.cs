using System;
using SymbolSource.Contract;

namespace SymbolSource.Support
{
    public enum SupportEnvironment
    {
        WebApp,
        WebJob,
    }

    public interface ISupportConfiguration
    {
        string IntercomAppId { get; set; }
        string IntercomApiKey { get; set; }
        string IntercomPrefix { get; }
        string InsightsInstrumentationKey { get; }
        string InsightsWebAppInstrumentationKey { get; set; }
        string InsightsWebJobInstrumentationKey { get; set; }
    }

    public class SupportConfiguration : ISupportConfiguration
    {
        private readonly SupportEnvironment environment;
        private readonly IConfigurationService configuration;
        private readonly IInstanceConfiguration instanceConfiguration;

        public SupportConfiguration(
            SupportEnvironment environment,
            IConfigurationService configuration,
            IInstanceConfiguration instanceConfiguration)
        {
            this.environment = environment;
            this.configuration = configuration;
            this.instanceConfiguration = instanceConfiguration;
        }

        public string IntercomAppId
        {
            get { return configuration["Support.IntercomAppId"]; }
            set { configuration["Support.IntercomAppId"] = value; }
        }

        public string IntercomApiKey
        {
            get { return configuration["Support.IntercomApiKey"]; }
            set { configuration["Support.IntercomApiKey"] = value; }
        }

        public string InsightsWebAppInstrumentationKey
        {
            get { return configuration["Support.InsightsWebAppInstrumentationKey"]; } 
            set { configuration["Support.InsightsWebAppInstrumentationKey"] = value; }
        }

        public string InsightsWebJobInstrumentationKey
        {
            get { return configuration["Support.InsightsWebJobInstrumentationKey"]; }
            set { configuration["Support.InsightsWebJobInstrumentationKey"] = value; }
        }

        public string IntercomPrefix
        {
            get { return instanceConfiguration.InstanceName; }
        }

        public string InsightsInstrumentationKey
        {
            get
            {
                switch (environment)
                {
                    case SupportEnvironment.WebApp:
                        return InsightsWebAppInstrumentationKey;
                    case SupportEnvironment.WebJob:
                        return InsightsWebJobInstrumentationKey;
                    default:
                        throw new ArgumentOutOfRangeException("environment", environment.ToString());
                }
            }
        }
    }
}
