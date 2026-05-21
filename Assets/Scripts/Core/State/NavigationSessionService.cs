using System;
using System.Collections.Generic;
using NavAR.Core.Entities;

namespace NavAR.Core.State
{
    public sealed class NavigationSessionService
    {
        private string _sessionId;
        private string _startQrId;
        private string _destinationId;
        private int _startFloorId;
        private DateTime _startUtc;
        private DateTime? _endUtc;
        private SessionStatus _status = SessionStatus.Unknown;
        private readonly List<string> _routeNodeIds = new List<string>();
        private readonly List<string> _visitedNodeIds = new List<string>();

        public bool HasActiveSession => !string.IsNullOrWhiteSpace(_sessionId);
        public string ActiveSessionId => _sessionId;
        public string StartQrId => _startQrId ?? string.Empty;
        public string DestinationId => _destinationId ?? string.Empty;
        public int StartFloorId => _startFloorId;
        public DateTime StartTimeUtc => _startUtc;
        public DateTime? EndTimeUtc => _endUtc;
        public SessionStatus Status => _status;

        public void StartSession(string startQrId, string destinationId, int floorId)
        {
            if (HasActiveSession)
            {
                return;
            }

            _sessionId = Guid.NewGuid().ToString("N");
            _startQrId = startQrId ?? string.Empty;
            _destinationId = destinationId ?? string.Empty;
            _startFloorId = floorId;
            _startUtc = DateTime.UtcNow;
            _endUtc = null;
            _status = SessionStatus.Started;
            _routeNodeIds.Clear();
            _visitedNodeIds.Clear();
        }

        public void SetRouteNodeIds(IReadOnlyList<string> nodeIds)
        {
            _routeNodeIds.Clear();
            if (nodeIds == null)
            {
                return;
            }

            for (var i = 0; i < nodeIds.Count; i++)
            {
                var nodeId = nodeIds[i];
                if (!string.IsNullOrWhiteSpace(nodeId))
                {
                    _routeNodeIds.Add(nodeId);
                }
            }
        }

        public void RecordVisitedNodeByIndex(int nodeIndex)
        {
            if (!HasActiveSession || nodeIndex < 0 || nodeIndex >= _routeNodeIds.Count)
            {
                return;
            }

            RecordVisitedNodeId(_routeNodeIds[nodeIndex]);
        }

        public void RecordVisitedNodeId(string nodeId)
        {
            if (!HasActiveSession || string.IsNullOrWhiteSpace(nodeId))
            {
                return;
            }

            if (!_visitedNodeIds.Contains(nodeId))
            {
                _visitedNodeIds.Add(nodeId);
            }
        }

        public void ResetVisitedNodes()
        {
            _visitedNodeIds.Clear();
        }

        public string[] GetVisitedNodeIds()
        {
            return _visitedNodeIds.ToArray();
        }

        public void MarkCompleted(SessionStatus status)
        {
            if (!HasActiveSession)
            {
                return;
            }

            _status = status;
            _endUtc = DateTime.UtcNow;
        }

        public void ClearSession()
        {
            _sessionId = null;
            _startQrId = null;
            _destinationId = null;
            _startFloorId = 0;
            _startUtc = default;
            _endUtc = null;
            _status = SessionStatus.Unknown;
            _routeNodeIds.Clear();
            _visitedNodeIds.Clear();
        }
    }
}
