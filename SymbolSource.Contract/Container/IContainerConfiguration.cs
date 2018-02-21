using System;

namespace SymbolSource.Contract.Container
{
    public interface IContainerConfiguration
    {
        Type SecurityType { get; }
        Type StorageType { get; }
        Type SchedulerType { get; }
    }

    public class ContainerConfiguration : IContainerConfiguration
    {
        private readonly IConfigurationService configuration;

        public ContainerConfiguration(IConfigurationService configuration)
        {
            this.configuration = configuration;
        }

        private const string SecurityTypeKey = "Container.SecurityType";
        private const string StorageTypeKey = "Container.StorageType";
        private const string SchedulerTypeKey = "Container.SchedulerType";

        public Type SecurityType
        {
            get { return Type.GetType(configuration[SecurityTypeKey]); }
            set { configuration[SecurityTypeKey] = value.AssemblyQualifiedName; }
        }

        public Type StorageType
        {
            get { return Type.GetType(configuration[StorageTypeKey]); }
            set { configuration[StorageTypeKey] = value.AssemblyQualifiedName; }
        }

        public Type SchedulerType
        {
            get { return Type.GetType(configuration[SchedulerTypeKey]); }
            set { configuration[SchedulerTypeKey] = value.AssemblyQualifiedName; }
        }
    }
}