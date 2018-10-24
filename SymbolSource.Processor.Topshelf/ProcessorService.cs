using System.Threading;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using SymbolSource.Contract.Scheduler;
using SymbolSource.Support;

namespace SymbolSource.Processor.Topshelf
{
    internal class ProcessorService
    {
        private readonly ISupportConfiguration _support;
        private readonly ISchedulerService _scheduler;
        private CancellationTokenSource _cancellationTokenSource;

        public ProcessorService(ISupportConfiguration support, ISchedulerService scheduler)
        {
            _support = support;
            _scheduler = scheduler;
        }

        public bool Start()
        {
            if (!string.IsNullOrWhiteSpace(_support.InsightsInstrumentationKey))
                TelemetryConfiguration.Active.InstrumentationKey = _support.InsightsInstrumentationKey;
            _cancellationTokenSource = ShutdownTokenSource();
            _scheduler.ListenAndProcess(_cancellationTokenSource.Token);
            
            return true;
        }

        public bool Stop()
        {
            _cancellationTokenSource.Cancel();

            return true;
        }

        private CancellationTokenSource ShutdownTokenSource()
        {
            var cancelSource = new CancellationTokenSource();

            System.Console.CancelKeyPress += (o, e) =>
            {
                cancelSource.Cancel();
                e.Cancel = true;
            };

            var shutdownWatcher = new WebJobsShutdownWatcher();
            return CancellationTokenSource.CreateLinkedTokenSource(new[]
            {
                shutdownWatcher.Token,
                cancelSource.Token
            });
        }
    }
}