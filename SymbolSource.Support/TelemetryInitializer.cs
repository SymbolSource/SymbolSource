using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace SymbolSource.Support
{
    public class TelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            if (HttpContext.Current != null)
            {
                telemetry.Context.User.UserAgent = HttpContext.Current.Request.UserAgent;
            }
        }
    }
}
