using System;

namespace SymbolSource.Contract.Support
{
    public interface ISupportService
    {
        void TrackEvent(UserInfo userInfo, string eventName, object metadata);
        void TrackException(Exception exception, object metadata);
        void TrackRequest(object name, DateTime start, TimeSpan duration, bool success);
        void TrackMetric(object name, double value, object metadata);
    }
}