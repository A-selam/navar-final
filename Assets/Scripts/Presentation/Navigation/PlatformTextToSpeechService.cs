using NavAR.Core.Interfaces;
using UnityEngine;

namespace NavAR.Presentation.Navigation
{
    public sealed class PlatformTextToSpeechService : ITextToSpeechService
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private const int QueueFlush = 0;

        private readonly AndroidJavaObject _textToSpeech;
        private bool _isReady;

        public PlatformTextToSpeechService()
        {
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    _textToSpeech = new AndroidJavaObject(
                        "android.speech.tts.TextToSpeech",
                        activity,
                        new TextToSpeechInitListener(this));
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Guidance][TTS] Android text-to-speech unavailable: {ex.Message}");
            }
        }
#endif

        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_textToSpeech == null || !_isReady)
            {
                Debug.Log($"[Guidance][TTS][Pending] {text}");
                return;
            }

            _textToSpeech.Call<int>("speak", text, QueueFlush, null, "NavAR.Guidance");
#else
            Debug.Log($"[Guidance][TTS][Sim] {text}");
#endif
        }

        public void Stop()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _textToSpeech?.Call<int>("stop");
#endif
        }

        public void Dispose()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_textToSpeech == null)
            {
                return;
            }

            _textToSpeech.Call<int>("stop");
            _textToSpeech.Call<int>("shutdown");
            _textToSpeech.Dispose();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private sealed class TextToSpeechInitListener : AndroidJavaProxy
        {
            private const int Success = 0;
            private readonly PlatformTextToSpeechService _service;

            public TextToSpeechInitListener(PlatformTextToSpeechService service)
                : base("android.speech.tts.TextToSpeech$OnInitListener")
            {
                _service = service;
            }

            public void onInit(int status)
            {
                _service._isReady = status == Success;
                if (!_service._isReady)
                {
                    Debug.LogWarning($"[Guidance][TTS] Initialization failed with status {status}.");
                }
            }
        }
#endif
    }
}
