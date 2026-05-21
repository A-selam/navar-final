using System;

namespace NavAR.Infrastructure.Backend
{
    [Serializable]
    public sealed class SessionStartPayload
    {
        public string eventType;
        public string timestampUtc;
        public string sessionId;
        public string startQrId;
        public string destinationId;
        public int floorId;
    }

    [Serializable]
    public sealed class RouteTakenPayload
    {
        public string eventType;
        public string timestampUtc;
        public string sessionId;
        public string destinationId;
        public string destinationName;
        public int floorId;
        public string[] visitedNodeIds;
        public string completionStatus;
    }

    [Serializable]
    public sealed class FeedbackPayload
    {
        public string eventType;
        public string timestampUtc;
        public string sessionId;
        public int rating;
        public string[] chips;
        public string comment;
        public string destinationName;
        public string destinationId;
        public int currentFloorId;
    }
}
