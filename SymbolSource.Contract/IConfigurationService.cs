using System;
using System.Collections.Generic;
using System.Configuration;

namespace SymbolSource.Contract
{
    public interface IConfigurationService 
    {
        string this[string key] { get; set;  }
    }

    public class DefaultConfigurationService : IConfigurationService
    {
        public string this[string key]
        {
            get
            {
                var value = ConfigurationManager.AppSettings[key];

                if (value == null)
                    throw new Exception(string.Format("Missing configuration key {0}", key));

                return value;
            }
            set
            {
                ConfigurationManager.AppSettings[key] = value;
            }
        }
    }

    public class MemoryConfigurationService : IConfigurationService
    {
        private readonly Dictionary<string, string> configuration;

        public MemoryConfigurationService()
        {
            configuration = new Dictionary<string, string>();
        }

        public string this[string key]
        {
            get
            {
                string value;

                if (!configuration.TryGetValue(key, out value))
                    throw new Exception(string.Format("Missing configuration key {0}", key));

                return value;
            }

            set
            {
                configuration[key] = value;
            }
        }
    }
}