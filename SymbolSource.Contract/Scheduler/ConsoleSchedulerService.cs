using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SymbolSource.Contract.Processor;
using SymbolSource.Contract.Storage;

namespace SymbolSource.Contract.Scheduler
{
    public class ConsoleSchedulerService : ISchedulerService
    {
        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        // ReSharper disable InconsistentNaming
        private const int VK_RETURN = 0x0D;
        private const int WM_KEYDOWN = 0x100;
        // ReSharper restore InconsistentNaming

        private readonly IStorageService storage;
        private readonly Lazy<IPackageProcessor> processor;

        public ConsoleSchedulerService(IStorageService storage, Lazy<IPackageProcessor> processor)
        {
            this.storage = storage;
            this.processor = processor;
        }

        public Task Signal(PackageMessage message)
        {
            return Task.Delay(TimeSpan.Zero);
        }

        public async void ListenAndProcess(CancellationToken cancellationToken)
        {
            Trace.TraceInformation("Starting console listener");

            cancellationToken.Register(() =>
            {
                var hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                PostMessage(hWnd, WM_KEYDOWN, VK_RETURN, 0);
            });

            while (!cancellationToken.IsCancellationRequested)
            {
                await Process(PackageState.New);
                await Process(PackageState.IndexingQueued);
                await Process(PackageState.DeletingQueued);
                await Process(PackageState.Partial);
                Console.ReadLine();
            }

            Trace.TraceInformation("Stopped console listener");
        }

        private async Task Process(PackageState packageState)
        {
            var packageNames = await storage.GetFeed(null).QueryPackages(null, packageState);

            foreach (var packageName in packageNames)
            {
                try
                {
                    await processor.Value.Process(new PackageMessage
                    {
                       PackageState = packageState,
                       PackageName = packageName,
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}