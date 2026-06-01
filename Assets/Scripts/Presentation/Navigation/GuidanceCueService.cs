using NavAR.Core.Interfaces;
using UnityEngine;

namespace NavAR.Presentation.Navigation
{
    public sealed class GuidanceCueService : IGuidanceCueService
    {
        private readonly INavigationInstructionPresenter _instructionPresenter;
        private readonly ITextToSpeechService _textToSpeechService;
        private string _lastSpokenMessage;

        public GuidanceCueService(INavigationInstructionPresenter instructionPresenter, ITextToSpeechService textToSpeechService)
        {
            _instructionPresenter = instructionPresenter;
            _textToSpeechService = textToSpeechService;
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

            if (ShouldDisplayInstruction(evt))
            {
                _instructionPresenter?.SetInstruction(evt.Message, evt.Severity, evt.DistanceMeters);
            }

            if (shouldSpeak)
            {
                Speak(evt.Message);
            }

            if (shouldVibrate)
            {
                Handheld.Vibrate();
            }
        }

        public void Reset()
        {
            _lastSpokenMessage = null;
            _textToSpeechService?.Stop();
            _instructionPresenter?.ClearInstruction();
        }

        private void Speak(string message)
        {
            if (string.IsNullOrWhiteSpace(message)
                || string.Equals(_lastSpokenMessage, message, System.StringComparison.Ordinal))
            {
                return;
            }

            _lastSpokenMessage = message;
            _textToSpeechService?.Speak(message);
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

        private static bool ShouldDisplayInstruction(GuidanceEvent evt)
        {
            return evt.EventType != GuidanceEventType.ReachedNode
                   && !string.IsNullOrWhiteSpace(evt.Message);
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
