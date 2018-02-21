using System;

namespace SymbolSource.Contract.Support
{
    public class NullSupportService : ISupportService
    {
        public void TrackEvent(UserInfo userInfo, string eventName, object metadata)
        {
        }

        public void TrackException(Exception exception, object metadata)
        {
        }

        public void TrackMetric(object name, double value, object metadata)
        {
        }

        public void TrackRequest(object name, DateTime start, TimeSpan duration, bool success)
        {
        }
    }
}