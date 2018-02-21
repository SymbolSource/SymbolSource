using System;
using System.Collections.Generic;

namespace SymbolSource.Contract.Security
{
    public interface IMyGetOrgSecurityConfiguration
    {
        string InstanceSalt { get; }
        string Host { get; }
        string Secret { get; }
    }

    public class MyGetOrgSecurityConfiguration : IMyGetOrgSecurityConfiguration
    {
        private readonly IConfigurationService configuration;
        private readonly IInstanceConfiguration instanceConfiguration;

        public MyGetOrgSecurityConfiguration(
            IConfigurationService configuration, 
            IInstanceConfiguration instanceConfiguration)
        {
            this.configuration = configuration;
            this.instanceConfiguration = instanceConfiguration;
        }

        public string InstanceSalt
        {
            get { return instanceConfiguration.InstanceSalt; }
        }

        public string Host
        {
            get { return configuration["MyGetOrgSecurity.Host"]; }
        }

        public string Secret
        {
            get { return configuration["MyGetOrgSecurity.Secret"]; }
        }
    }
}