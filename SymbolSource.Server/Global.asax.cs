using System;
using System.Web.Http;
using Microsoft.ApplicationInsights.Extensibility;
using SymbolSource.Support;

namespace SymbolSource.Server
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            GlobalConfiguration.Configure(WebApiConfiguration.Register);

            var support = GlobalConfiguration.Configuration.GetService<ISupportConfiguration>();

            if (!string.IsNullOrWhiteSpace(support.InsightsInstrumentationKey))
                TelemetryConfiguration.Active.InstrumentationKey = support.InsightsInstrumentationKey;
        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }

    public static class ConfigurationExtensions
    {
        public static T GetService<T>(this HttpConfiguration configuration)
        {
            return (T)configuration.DependencyResolver.GetService(typeof(T));
        }
    }
}