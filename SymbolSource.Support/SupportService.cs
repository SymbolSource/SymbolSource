using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using IntercomDotNet;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using SymbolSource.Contract;
using SymbolSource.Contract.Support;

namespace SymbolSource.Support
{
    public class SupportService : ISupportService, IDisposable
    {
        private readonly ISupportConfiguration configuration;
        private readonly IntercomClient intercom;
        private readonly TelemetryConfiguration insightsConfiguration;
        private readonly TelemetryClient insights;

        public SupportService(ISupportConfiguration configuration)
        {
            this.configuration = configuration;

            if (!string.IsNullOrWhiteSpace(configuration.IntercomAppId)
                && !string.IsNullOrWhiteSpace(configuration.IntercomApiKey))
                intercom = IntercomClient.GetClient(
                    configuration.IntercomAppId,
                    configuration.IntercomApiKey);

            if (!string.IsNullOrWhiteSpace(TelemetryConfiguration.Active.InstrumentationKey))
            {
                insights = new TelemetryClient(TelemetryConfiguration.Active);
            }
            else if (!string.IsNullOrWhiteSpace(configuration.InsightsInstrumentationKey))
            {
                insightsConfiguration = TelemetryConfiguration.CreateDefault();
                insightsConfiguration.InstrumentationKey = configuration.InsightsInstrumentationKey;
                insights = new TelemetryClient(insightsConfiguration);
            }
        }


        public void Dispose()
        {
            insightsConfiguration?.Dispose();
        }

        private void CatchAndTrack(string message, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Trace.TraceWarning("{0}:\n{1}", message, e);
                TrackException(e, new { message });
            }
        }

        public void TrackEvent(UserInfo userInfo, string eventName, object metadata)
        {
            if (intercom == null) 
                return;

            CatchAndTrack("Failed to post to Intercom",
                () =>
                {
                    var userName = GetPrefixedName(userInfo.UserName);

                    if (userInfo.UserHandle != null)
                    {
                        if (userInfo.UserHandle.StartsWith("@"))
                        {
                            intercom.Users.Post(new
                            {
                                user_id = userName
                            });
                        }
                        else if (userInfo.UserHandle.Contains("@"))
                        {
                            intercom.Users.Post(new
                            {
                                user_id = userName,
                                email = userInfo.UserHandle,
                            });
                        }
                    }
                    else
                    {
                        intercom.Users.Post(new
                        {
                            user_id = userName
                        });
                    }

                    intercom.Events.Post(new
                    {
                        user_id = userName,
                        event_name = eventName,
                        created_at = ToEpoch(DateTime.Now),
                        metadata
                    });
                });
        }

        private string GetPrefixedName(string name)
        {
            return string.Format("{0}/{1}", configuration.IntercomPrefix, name);
        }

        private static int ToEpoch(DateTime dateTime)
        {
            return (int)(dateTime - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        private void CatchAndTrace(string message, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Trace.TraceError("{0}:\n{1}", message, e);
            }
        }

        public void TrackException(Exception exception, object metadata)
        {
            if (insights == null) 
                return;

            var properties = ToDictionary(metadata);

            CatchAndTrace("Failed to post to Insights",
                () => insights.TrackException(exception, properties));
        }

        private static Dictionary<string, string> ToDictionary(object metadata)
        {
            if (metadata == null)
                return null;

            return metadata
                .GetType()
                .GetProperties()
                .ToDictionary(
                    p => p.Name,
                    p => p.GetValue(metadata, null).ToString());
        }

        public void TrackRequest(object name, DateTime start, TimeSpan duration, bool success)
        {
            if (insights == null) 
                return;

            CatchAndTrack("Failed to post to Insights",
                () => insights.TrackRequest(name.ToString(), new DateTimeOffset(start), duration, null, success));

        }

        public void TrackMetric(object name, double value, object metadata)
        {
            if (insights == null)
                return;

            var properties = ToDictionary(metadata);

            CatchAndTrack("Failed to post to Insights",
                () => insights.TrackMetric(name.ToString(), value, properties));
        }
    }
}