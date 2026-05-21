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

            if (!string.IsNullOrWhiteSpace(evt.Message))
            {
                _instructionPresenter?.SetInstruction(evt.Message, evt.Severity, evt.DistanceMeters);
            }

            if (voiceEnabled)
            {
                #if UNITY_EDITOR
                Debug.Log($"[Guidance][VoiceSim][Editor] {evt.Message}");
                #else
                Debug.Log($"[Guidance][Voice] {evt.Message}");
                #endif
            }

            if (hapticsEnabled && (evt.EventType == GuidanceEventType.ApproachingNode || evt.EventType == GuidanceEventType.ReachedNode || evt.EventType == GuidanceEventType.OffRouteDetected))
            {
                Handheld.Vibrate();
            }
        }

        public void Reset()
        {
            _instructionPresenter?.ClearInstruction();
        }
    }
}
