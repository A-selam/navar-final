using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NavAR.Infrastructure.Backend
{
    public sealed class BackendApiClient
    {
        private const string DefaultSessionStartPath = "navigation/session/start";
        private const string DefaultRoutePath = "navigation/route";
        private const string DefaultFeedbackPath = "navigation/feedback";

        private readonly MonoBehaviour _runner;
        private readonly string _baseUrl;
        private readonly string _sessionStartPath;
        private readonly string _routePath;
        private readonly string _feedbackPath;
        private readonly IBackendEventQueue _queue;
        private bool _isFlushing;
        private Coroutine _autoFlushRoutine;

        public BackendApiClient(
            MonoBehaviour runner,
            string baseUrl,
            IBackendEventQueue queue = null,
            string sessionStartPath = null,
            string routePath = null,
            string feedbackPath = null)
        {
            _runner = runner;
            _baseUrl = baseUrl ?? string.Empty;
            _queue = queue;
            _sessionStartPath = string.IsNullOrWhiteSpace(sessionStartPath) ? DefaultSessionStartPath : sessionStartPath;
            _routePath = string.IsNullOrWhiteSpace(routePath) ? DefaultRoutePath : routePath;
            _feedbackPath = string.IsNullOrWhiteSpace(feedbackPath) ? DefaultFeedbackPath : feedbackPath;
        }

        public void StartAutoFlush(float intervalSeconds = 5f)
        {
            if (_runner == null || _autoFlushRoutine != null)
            {
                return;
            }

            _autoFlushRoutine = _runner.StartCoroutine(AutoFlushRoutine(Mathf.Max(1f, intervalSeconds)));
        }

        public void SendSessionStart(SessionStartPayload payload)
        {
            SendJsonPayload(payload, _sessionStartPath, "SessionStart");
        }

        public void SendRouteTaken(RouteTakenPayload payload)
        {
            SendJsonPayload(payload, _routePath, "RouteTaken");
        }

        public void SendFeedback(FeedbackPayload payload)
        {
            SendJsonPayload(payload, _feedbackPath, "Feedback");
        }

        private void SendJsonPayload<T>(T payload, string endpoint, string label)
        {
            if (payload == null)
            {
                Debug.LogWarning($"[BackendApiClient] {label} payload is null. Skipping request.");
                return;
            }

            if (_runner == null)
            {
                Debug.LogWarning($"[BackendApiClient] No runner available for {label} request.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_baseUrl))
            {
                Debug.LogWarning($"[BackendApiClient] Base URL is empty. Skipping {label} request.");
                return;
            }

            TryFlushQueue();

            var json = JsonUtility.ToJson(payload, true);
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                EnqueuePayload(label, endpoint, json);
                return;
            }

            var url = CombineUrl(_baseUrl, endpoint);
            _runner.StartCoroutine(PostJson(url, json, label, endpoint));
        }

        private static string CombineUrl(string baseUrl, string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return baseUrl.TrimEnd('/');
            }

            if (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return endpoint;
            }

            return $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        }

        private IEnumerator PostJson(string url, string json, string label, string endpoint)
        {
            var body = Encoding.UTF8.GetBytes(json);

            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = 8;
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[BackendApiClient] {label} request failed ({request.responseCode}): {request.error}");
                    if (request.result == UnityWebRequest.Result.ConnectionError
                        || request.result == UnityWebRequest.Result.DataProcessingError
                        || request.responseCode == 0)
                    {
                        EnqueuePayload(label, endpoint, json);
                    }
                    yield break;
                }

                Debug.Log($"[BackendApiClient] {label} request succeeded ({request.responseCode}).");
            }
        }

        private void EnqueuePayload(string eventType, string endpoint, string json)
        {
            if (_queue == null)
            {
                Debug.LogWarning($"[BackendApiClient] Queue unavailable. Dropping {eventType} payload.");
                return;
            }

            _queue.Enqueue(eventType, endpoint, json);
            Debug.Log($"[BackendApiClient] Queued {eventType} payload for later send.");
        }

        private void TryFlushQueue()
        {
            if (_queue == null || _isFlushing || _runner == null)
            {
                return;
            }

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                return;
            }

            if (_queue.Count() == 0)
            {
                return;
            }

            _runner.StartCoroutine(FlushQueueRoutine());
        }

        private IEnumerator FlushQueueRoutine()
        {
            _isFlushing = true;

            while (Application.internetReachability != NetworkReachability.NotReachable)
            {
                var batch = _queue.DequeueBatch(10);
                if (batch == null || batch.Count == 0)
                {
                    break;
                }

                foreach (var entry in batch)
                {
                    _queue.MarkAttempt(entry.Id);
                    var url = CombineUrl(_baseUrl, entry.Endpoint);
                    yield return PostQueuedJson(url, entry);

                    if (Application.internetReachability == NetworkReachability.NotReachable)
                    {
                        break;
                    }
                }
            }

            _isFlushing = false;
        }

        private IEnumerator PostQueuedJson(string url, BackendQueuedEvent entry)
        {
            var body = Encoding.UTF8.GetBytes(entry.PayloadJson);
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = 8;
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    _queue.Delete(entry.Id);
                    Debug.Log($"[BackendApiClient] Flushed queued {entry.EventType} payload ({request.responseCode}).");
                }
                else
                {
                    Debug.LogWarning($"[BackendApiClient] Failed to flush queued {entry.EventType} ({request.responseCode}): {request.error}");
                    if (request.result == UnityWebRequest.Result.ProtocolError)
                    {
                        _queue.Delete(entry.Id);
                    }
                }
            }
        }

        private IEnumerator AutoFlushRoutine(float intervalSeconds)
        {
            while (true)
            {
                TryFlushQueue();
                yield return new WaitForSeconds(intervalSeconds);
            }
        }
    }
}
