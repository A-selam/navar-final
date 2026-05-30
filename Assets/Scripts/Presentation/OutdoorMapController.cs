using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

#if GREE_UNITY_WEBVIEW
using Gree.UnityWebView;
#endif

namespace NavAR.Presentation
{
    public sealed class OutdoorMapController : MonoBehaviour
    {
        [Header("Map")]
        [SerializeField] private string outdoorMapUrl = "https://outside-navigation.vercel.app/";

        [Header("Layout")]
        [SerializeField] private bool useSafeAreaMargins = true;
        [SerializeField] private int extraLeftMarginPixels;
        [SerializeField] private int extraTopMarginPixels = 24;
        [SerializeField] private int extraRightMarginPixels;
        [SerializeField] private int extraBottomMarginPixels = 24;
        [SerializeField] private bool refreshMapLayoutAfterLoad = true;

        [Header("AR")]
        [SerializeField] private ARSession arSession;

#if GREE_UNITY_WEBVIEW
        private WebViewObject _webViewObject;
#else
        private Component _webViewObject;
#endif
        private Coroutine _openRoutine;
        private bool _arSessionWasEnabled;
        private bool _isOpen;
        public bool IsOpen => _isOpen || _openRoutine != null;

        public void OpenOutdoorMap()
        {
            Debug.Log("OutdoorMapController: OpenOutdoorMap requested.");

            if (_openRoutine != null)
            {
                StopCoroutine(_openRoutine);
            }

            _openRoutine = StartCoroutine(OpenOutdoorMapRoutine());
        }

        public void CloseOutdoorMap()
        {
            if (_openRoutine != null)
            {
                StopCoroutine(_openRoutine);
                _openRoutine = null;
            }

            DestroyWebView();
            ResumeArSession();
            _isOpen = false;
        }

        public bool TryCloseOutdoorMap()
        {
            if (!IsOpen)
            {
                return false;
            }

            CloseOutdoorMap();
            return true;
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                CloseOutdoorMap();
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseOutdoorMap();
            }
#endif
        }

        private IEnumerator OpenOutdoorMapRoutine()
        {
            if (string.IsNullOrWhiteSpace(outdoorMapUrl))
            {
                Debug.LogError("OutdoorMapController: Outdoor map URL is not set.");
                _openRoutine = null;
                yield break;
            }

            yield return RequestLocationPermissionBeforeWebViewLoad();

            if (!HasLocationPermission())
            {
                Debug.LogWarning("OutdoorMapController: Location permission was denied. Outdoor map was not loaded.");
                ShowDeviceToast("Location permission is required for outdoor navigation.");
                _openRoutine = null;
                yield break;
            }

            PauseArSession();
            CreateAndLoadWebView();
            _isOpen = true;
            _openRoutine = null;
        }

        private IEnumerator RequestLocationPermissionBeforeWebViewLoad()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                yield break;
            }

            var completed = false;
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += permissionName => completed = true;
            callbacks.PermissionDenied += permissionName => completed = true;
            callbacks.PermissionDeniedAndDontAskAgain += permissionName => completed = true;

            Permission.RequestUserPermission(Permission.FineLocation, callbacks);

            while (!completed)
            {
                yield return null;
            }
#elif UNITY_IOS && !UNITY_EDITOR
            if (Input.location.status == LocationServiceStatus.Stopped)
            {
                Input.location.Start();
            }

            while (Input.location.status == LocationServiceStatus.Initializing)
            {
                yield return null;
            }
#else
            yield return null;
#endif
        }

        private bool HasLocationPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Permission.HasUserAuthorizedPermission(Permission.FineLocation);
#elif UNITY_IOS && !UNITY_EDITOR
            return Input.location.status == LocationServiceStatus.Running;
#else
            return true;
#endif
        }

        private void PauseArSession()
        {
            if (arSession == null)
            {
                arSession = FindObjectOfType<ARSession>();
            }

            if (arSession == null)
            {
                Debug.LogWarning("OutdoorMapController: No ARSession found to pause.");
                return;
            }

            _arSessionWasEnabled = arSession.enabled;
            arSession.enabled = false;
        }

        private void ResumeArSession()
        {
            if (arSession != null && _arSessionWasEnabled)
            {
                arSession.enabled = true;
            }
        }

        private void CreateAndLoadWebView()
        {
            DestroyWebView();

#if GREE_UNITY_WEBVIEW
            _webViewObject = gameObject.AddComponent<WebViewObject>();
            _webViewObject.Init(
                cb: msg => Debug.Log($"OutdoorMap WebView callback: {msg}"),
                err: msg => Debug.LogError($"OutdoorMap WebView error: {msg}"),
                httpErr: msg => Debug.LogError($"OutdoorMap WebView HTTP error: {msg}"),
                started: msg => Debug.Log($"OutdoorMap WebView started: {msg}"),
                ld: OnWebViewLoaded,
                zoom: false,
                androidForceDarkMode: 1,
                enableWKWebView: true
            );

            ApplySafeAreaMargins();
            ConfigureWebViewForMapRendering();
            _webViewObject.SetVisibility(true);
            _webViewObject.LoadURL(outdoorMapUrl.Trim());
            StartCoroutine(RefreshMapLayoutAfterFrames());
#else
            var message = "GREE unity-webview is not compiled for this build target. Add GREE_UNITY_WEBVIEW to the active platform's Scripting Define Symbols.";
            Debug.LogError($"OutdoorMapController: {message}");
            ShowDeviceToast(message);
#endif
        }

        private void ApplySafeAreaMargins()
        {
#if GREE_UNITY_WEBVIEW
            if (_webViewObject == null)
            {
                return;
            }

            var safeArea = useSafeAreaMargins ? Screen.safeArea : new Rect(0f, 0f, Screen.width, Screen.height);
            var left = Mathf.RoundToInt(safeArea.xMin) + extraLeftMarginPixels;
            var top = Mathf.RoundToInt(Screen.height - safeArea.yMax) + extraTopMarginPixels;
            var right = Mathf.RoundToInt(Screen.width - safeArea.xMax) + extraRightMarginPixels;
            var bottom = Mathf.RoundToInt(safeArea.yMin) + extraBottomMarginPixels;

            _webViewObject.SetMargins(
                Mathf.Max(0, left),
                Mathf.Max(0, top),
                Mathf.Max(0, right),
                Mathf.Max(0, bottom));
#endif
        }

#if GREE_UNITY_WEBVIEW
        private void ConfigureWebViewForMapRendering()
        {
            if (_webViewObject == null)
            {
                return;
            }

            _webViewObject.SetScrollbarsVisibility(false);
            _webViewObject.SetTextZoom(100);
            _webViewObject.SetMixedContentMode(0);
        }

        private void OnWebViewLoaded(string url)
        {
            Debug.Log($"OutdoorMap WebView loaded: {url}");
            StartCoroutine(RefreshMapLayoutAfterFrames());
        }

        private IEnumerator RefreshMapLayoutAfterFrames()
        {
            if (!refreshMapLayoutAfterLoad)
            {
                yield break;
            }

            yield return null;
            RefreshMapLayoutInWebView();
            yield return new WaitForSecondsRealtime(0.25f);
            RefreshMapLayoutInWebView();
            yield return new WaitForSecondsRealtime(0.75f);
            RefreshMapLayoutInWebView();
        }

        private void RefreshMapLayoutInWebView()
        {
            if (_webViewObject == null)
            {
                return;
            }

            _webViewObject.EvaluateJS(
                "(function(){" +
                "function forceLeafletCanvasRenderer(){" +
                "var L=window.L||window.leaflet;" +
                "if(!L||!L.canvas){return false;}" +
                "if(L.Map&&L.Map.prototype&&L.Map.prototype.options){L.Map.prototype.options.preferCanvas=true;}" +
                "var canvasRenderer=L.canvas({padding:0.5});" +
                "if(L.Polyline&&L.Polyline.prototype){L.Polyline.prototype.options.renderer=canvasRenderer;}" +
                "if(L.Polygon&&L.Polygon.prototype){L.Polygon.prototype.options.renderer=canvasRenderer;}" +
                "if(L.Circle&&L.Circle.prototype){L.Circle.prototype.options.renderer=canvasRenderer;}" +
                "if(L.CircleMarker&&L.CircleMarker.prototype){L.CircleMarker.prototype.options.renderer=canvasRenderer;}" +
                "if(window.map&&window.map.options){window.map.options.preferCanvas=true;}" +
                "if(window.__navarMap&&window.__navarMap.options){window.__navarMap.options.preferCanvas=true;}" +
                "return true;" +
                "}" +
                "forceLeafletCanvasRenderer();" +
                "var canvasAttempts=0;" +
                "var canvasInterval=setInterval(function(){canvasAttempts++;if(forceLeafletCanvasRenderer()||canvasAttempts>40){clearInterval(canvasInterval);}},250);" +
                "function fire(){" +
                "window.dispatchEvent(new Event('resize'));" +
                "window.dispatchEvent(new Event('orientationchange'));" +
                "if(window.map&&window.map.invalidateSize){window.map.invalidateSize(true);}" +
                "if(window.map&&window.map.resize){window.map.resize();}" +
                "if(window.__navarMap&&window.__navarMap.invalidateSize){window.__navarMap.invalidateSize(true);}" +
                "if(window.__navarMap&&window.__navarMap.resize){window.__navarMap.resize();}" +
                "if(window.google&&window.google.maps&&window.__navarMap){window.google.maps.event.trigger(window.__navarMap,'resize');}" +
                "}" +
                "fire();setTimeout(fire,100);setTimeout(fire,350);" +
                "})()");
        }
#endif

        private void DestroyWebView()
        {
            if (_webViewObject == null)
            {
                return;
            }

#if GREE_UNITY_WEBVIEW
            _webViewObject.SetVisibility(false);
#endif
            Destroy(_webViewObject);
            _webViewObject = null;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_isOpen)
            {
                ApplySafeAreaMargins();
#if GREE_UNITY_WEBVIEW
                StartCoroutine(RefreshMapLayoutAfterFrames());
#endif
            }
        }

        private void OnDestroy()
        {
            CloseOutdoorMap();
        }

        private static void ShowDeviceToast(string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                using var toastClass = new AndroidJavaClass("android.widget.Toast");
                using var toast = toastClass.CallStatic<AndroidJavaObject>(
                    "makeText",
                    activity,
                    message,
                    toastClass.GetStatic<int>("LENGTH_LONG"));
                toast.Call("show");
            }));
#endif
        }
    }
}
