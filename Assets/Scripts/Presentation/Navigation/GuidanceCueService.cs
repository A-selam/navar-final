using NavAR.Core.Interfaces;
using UnityEngine;

namespace NavAR.Presentation.Navigation
{
    public sealed class GuidanceCueService : IGuidanceCueService
    {
        private readonly INavigationInstructionPresenter _instructionPresenter;

        public GuidanceCueService(INavigationInstructionPresenter instructionPresenter)
        {
            _instructionPresenter = instructionPresenter;
        }

        public void HandleGuidanceEvent(GuidanceEvent evt, bool voiceEnabled, bool hapticsEnabled)
        {
            if (evt == null)
            {
                return;
            }

            var isDirectionalCue = IsDirectionalCue(evt);
            var shouldSpeak = voiceEnabled && ShouldEmitVoice(evt, isDirectionalCue);
            var shouldVibrate = hapticsEnabled && ShouldEmitHaptics(evt, isDirectionalCue);

            if (!string.IsNullOrWhiteSpace(evt.Message))
            {
                _instructionPresenter?.SetInstruction(evt.Message, evt.Severity, evt.DistanceMeters);
            }

            if (shouldSpeak)
            {
                #if UNITY_EDITOR
                Debug.Log($"[Guidance][VoiceSim][Editor] {evt.Message}");
                #else
                Debug.Log($"[Guidance][Voice] {evt.Message}");
                #endif
            }

            if (shouldVibrate)
            {
                Handheld.Vibrate();
            }
        }

        public void Reset()
        {
            _instructionPresenter?.ClearInstruction();
        }

        private static bool IsDirectionalCue(GuidanceEvent evt)
        {
            if (evt == null || string.IsNullOrWhiteSpace(evt.Message))
            {
                return false;
            }

            var message = evt.Message.ToLowerInvariant();
            return message.Contains("turn left")
                   || message.Contains("turn right")
                   || message.Contains("u-turn")
                   || message.Contains("uturn");
        }

        private static bool ShouldEmitVoice(GuidanceEvent evt, bool isDirectionalCue)
        {
            switch (evt.EventType)
            {
                case GuidanceEventType.ApproachingNode:
                case GuidanceEventType.TurnInstructionReady:
                    return isDirectionalCue;
                case GuidanceEventType.OffRouteDetected:
                case GuidanceEventType.RouteRecalculated:
                case GuidanceEventType.DestinationReached:
                    return true;
                default:
                    return false;
            }
        }

        private static bool ShouldEmitHaptics(GuidanceEvent evt, bool isDirectionalCue)
        {
            switch (evt.EventType)
            {
                case GuidanceEventType.ApproachingNode:
                case GuidanceEventType.TurnInstructionReady:
                    return isDirectionalCue;
                case GuidanceEventType.OffRouteDetected:
                case GuidanceEventType.RouteRecalculated:
                case GuidanceEventType.DestinationReached:
                    return true;
                default:
                    return false;
            }
        }
    }
}
