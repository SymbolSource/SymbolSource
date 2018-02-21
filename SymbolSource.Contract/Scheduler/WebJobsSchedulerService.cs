using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core.Lifetime;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using SymbolSource.Contract.Processor;

namespace SymbolSource.Contract.Scheduler
{
    public class WebJobsSchedulerService : ISchedulerService, IJobActivator
    {
        internal const string QueueName = "packages";

        private readonly IWebJobsSchedulerConfiguration configuration;
        private readonly ILifetimeScope scope;
        private readonly CloudQueueClient queueClient;

        public WebJobsSchedulerService(IWebJobsSchedulerConfiguration configuration, ILifetimeScope scope)
        {
            this.configuration = configuration;
            this.scope = scope;

            var account = CloudStorageAccount.Parse(configuration.StorageConnectionString);
            queueClient = account.CreateCloudQueueClient();
        }

        public async Task Signal(PackageMessage message)
        {
            var queue = queueClient.GetQueueReference(QueueName);
            await queue.CreateIfNotExistsAsync();
            
            var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(message));
            await queue.AddMessageAsync(queueMessage);
        }

        public void ListenAndProcess(CancellationToken cancellationToken)
        {
            var config = new JobHostConfiguration();
            config.JobActivator = this;
            config.DashboardConnectionString = configuration.DashboardConnectionString;
            config.StorageConnectionString = configuration.StorageConnectionString;
            config.Queues.BatchSize = configuration.BatchSize; //16
            config.Queues.MaxDequeueCount = 1; //5
            var host = new JobHost(config);
            host.Start();
            cancellationToken.WaitHandle.WaitOne();
            Trace.TraceInformation("Stopping job host");
            host.Stop();
        }

        public T CreateInstance<T>()
        {
            var requestScope = scope.BeginLifetimeScope(
                MatchingScopeLifetimeTags.RequestLifetimeScopeTag,
                builder => builder.RegisterType<T>());

            return requestScope.Resolve<T>();
        }
    }

    public class WebJobsSchedulerServiceJob : IDisposable
    {
        private readonly ILifetimeScope scope;
        private readonly IPackageProcessor processor;

        public WebJobsSchedulerServiceJob(ILifetimeScope scope, IPackageProcessor processor)
        {
            this.scope = scope;
            this.processor = processor;
        }

        public void Dispose()
        {
            scope.Dispose();
        }

        public async Task Process([QueueTrigger(WebJobsSchedulerService.QueueName)]PackageMessage message)
        {
            await processor.Process(message);
        }
    }
}