using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace SymbolSource.Support
{
    public class TelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor next;

        public TelemetryProcessor(ITelemetryProcessor next)
        {
            this.next = next;
        }

        public void Process(ITelemetry item)
        {
            if (item is RequestTelemetry request)
            {
                if (IsNotFound(request) && IsSymbol(request))
                {
                    request.Success = true;
                }
            }

            next.Process(item);
        }

        private static bool IsNotFound(RequestTelemetry request)
        {
            return request.Success.HasValue
                   && request.Success.Value == false
                   && request.ResponseCode == "404";
        }

        private static readonly string[] SymbolPaths = new[]
        {
            ".pdb",
            ".pd_",
            "/file.ptr",
            "/index2.txt"
        };

        private static bool IsSymbol(RequestTelemetry request)
        {
            return SymbolPaths.Any(path => request.Url.AbsolutePath.EndsWith(path));
        }
    }
}
