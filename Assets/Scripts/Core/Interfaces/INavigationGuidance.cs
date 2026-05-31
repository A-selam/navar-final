using System;
using System.Collections.Generic;
using UnityEngine;

namespace NavAR.Core.Interfaces
{
    public enum GuidanceSeverity
    {
        Info = 0,
        Caution = 1,
        Warning = 2
    }

    public enum GuidanceEventType
    {
        ApproachingNode = 0,
        ReachedNode = 1,
        TurnInstructionReady = 2,
        SegmentReversed = 3,
        OffRouteDetected = 4,
        RouteRecalculated = 5,
        DestinationReached = 6,
        RouteInstructionUpdated = 7
    }

    public sealed class GuidanceEvent
    {
        public GuidanceEventType EventType;
        public string Message;
        public GuidanceSeverity Severity;
        public float? DistanceMeters;
        public int NodeIndex = -1;
        public Vector3 UserPosition;
    }

    public interface INavigationInstructionPresenter
    {
        void SetInstruction(string text, GuidanceSeverity severity, float? distanceMeters);
        void ClearInstruction();
    }

    public interface IGuidanceCueService
    {
        void HandleGuidanceEvent(GuidanceEvent evt, bool voiceEnabled, bool hapticsEnabled);
        void Reset();
    }

    public interface ITextToSpeechService : IDisposable
    {
        void Speak(string text);
        void Stop();
    }

    public interface INavigationProgressTracker
    {
        event Action<GuidanceEvent> OnGuidanceEvent;
        bool HasActiveRoute { get; }
        void InitializeRoute(List<Vector3> routeCorners, bool canCompleteNavigation = true);
        void Reset();
        void Tick(Vector3 userWorldPosition, Vector3 userForward, float deltaTime);
        List<Vector3> GetDynamicRenderPath(Vector3 userWorldPosition);
    }
}
