using System;
using System.Collections.Generic;

namespace NavAR.Infrastructure.Backend
{
    [Serializable]
    public sealed class SessionStartPayload
    {
        public string session_id;
        public string qr_id;
        public string destination_node_id;
    }

    [Serializable]
    public sealed class NavigationSessionPayload
    {
        public string session_id;
        public string qr_id;
        public int[] visited_node_ids;
        public string destination_node_id;
        public string ended_at;
    }

    [Serializable]
    public sealed class MobileSyncPayload
    {
        public List<NavigationSessionPayload> sessions;
    }

    [Serializable]
    public sealed class FeedbackPayload
    {
        public string session_id;
        public string[] chips;
        public string comment;
        public int rating;
    }
}
