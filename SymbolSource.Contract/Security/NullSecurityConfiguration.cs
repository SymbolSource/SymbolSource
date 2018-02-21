using System;
using System.Collections.Generic;

namespace SymbolSource.Contract.Security
{
    public interface INullSecurityConfiguration
    {
        bool AllowNamedFeeds { get; }
        IEnumerable<string> PushApiKeys { get; }
        string InstanceSalt { get; }
    }

    public class NullSecurityConfiguration : INullSecurityConfiguration
    {
        private readonly IConfigurationService configuration;
        private readonly IInstanceConfiguration instanceConfiguration;

        public NullSecurityConfiguration(
            IConfigurationService configuration, 
            IInstanceConfiguration instanceConfiguration)
        {
            this.configuration = configuration;
            this.instanceConfiguration = instanceConfiguration;
        }

        public bool AllowNamedFeeds
        {
            get
            {
                bool value;

                if (!bool.TryParse(configuration["NullSecurity.AllowNamedFeeds"], out value))
                    return false;

                return value;
            }
            set
            {
                configuration["NullSecurity.AllowNamedFeeds"] = value.ToString();
            }
        }

        public IEnumerable<string> PushApiKeys
        {
            get
            {
                var value = configuration["NullSecurity.PushApiKeys"];

                if (string.IsNullOrEmpty(value))
                    return new string[0];

                return value.Split(new[] {',', ';', ' '}, StringSplitOptions.RemoveEmptyEntries);
            }
            set
            {
                configuration["NullSecurity.PushApiKeys"] = string.Join(", ", value);
            }
        }

        public string InstanceSalt
        {
            get { return instanceConfiguration.InstanceSalt; }
        }
    }
}