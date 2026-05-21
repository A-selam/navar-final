using System.Collections.Generic;

namespace NavAR.Infrastructure.Backend
{
    public sealed class BackendQueuedEvent
    {
        public int Id;
        public string EventType;
        public string Endpoint;
        public string PayloadJson;
        public int AttemptCount;
    }

    public interface IBackendEventQueue
    {
        void Enqueue(string eventType, string endpoint, string payloadJson);
        List<BackendQueuedEvent> DequeueBatch(int maxCount);
        void MarkAttempt(int id);
        void Delete(int id);
        int Count();
    }
}
