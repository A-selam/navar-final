using System;
using NavAR.Core.State;
using NavAR.Core.Interfaces;
using NavAR.Presentation.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace NavAR.Presentation.Presenters
{
    public sealed class SplashScreenPresenter : IScreenPresenter
    {
        private readonly VisualTreeAsset _asset;
        public AppState State => AppState.Splash;
        public SplashScreenPresenter(VisualTreeAsset asset) { _asset = asset; }
        public void Show(VisualElement container) => RenderAsset(container, _asset);
        private static void RenderAsset(VisualElement container, VisualTreeAsset asset)
        {
            var instance = asset.Instantiate();
            instance.style.flexGrow = 1;
            container.Add(instance);
        }
    }

    public sealed class HomeScreenPresenter : IScreenPresenter
    {
        private readonly VisualTreeAsset _asset;
        private readonly Action<AppState> _setState;
        private readonly Action _onViewHelp;
        private readonly Action _onLaunchTutorial;
        public AppState State => AppState.Home;
        public HomeScreenPresenter(VisualTreeAsset asset, Action<AppState> setState, Action onViewHelp, Action onLaunchTutorial)
        {
            _asset = asset; _setState = setState; _onViewHelp = onViewHelp; _onLaunchTutorial = onLaunchTutorial;
        }
        public void Show(VisualElement container)
        {
            var instance = _asset.Instantiate();
            instance.style.flexGrow = 1;
            container.Add(instance);
            ScreenBinders.WireHome(container, _setState, _onViewHelp, _onLaunchTutorial);
        }
    }

    public sealed class ExploreScreenPresenter : IScreenPresenter
    {
        private readonly VisualTreeAsset _asset;
        private readonly Action<AppState> _setState;
        private readonly Action _populateDestinations;
        public AppState State => AppState.Explore;
        public ExploreScreenPresenter(VisualTreeAsset asset, Action<AppState> setState, Action populateDestinations)
        {
            _asset = asset; _setState = setState; _populateDestinations = populateDestinations;
        }
        public void Show(VisualElement container)
        {
            var instance = _asset.Instantiate();
            instance.style.flexGrow = 1;
            container.Add(instance);
            _populateDestinations?.Invoke();
            ScreenBinders.WireExplore(container, _setState);
        }
    }

    public sealed class PermissionScreenPresenter : IScreenPresenter
    {
        private readonly VisualTreeAsset _asset;
        private readonly Action<AppState> _setState;
        private readonly Action _requestPermission;
        public AppState State => AppState.Permission;
        public PermissionScreenPresenter(VisualTreeAsset asset, Action<AppState> setState, Action requestPermission)
        {
            _asset = asset; _setState = setState; _requestPermission = requestPermission;
        }
        public void Show(VisualElement container)
        {
            var instance = _asset.Instantiate();
            instance.style.flexGrow = 1;
            container.Add(instance);
            ScreenBinders.WirePermission(container, _setState, _requestPermission);
        }
    }

    public sealed class QrScanScreenPresenter : IScreenPresenter, IHideablePresenter
    {
        private readonly VisualTreeAsset _asset;
        private readonly IQrScannerService _qrScannerService;
        private readonly Action<string> _onQrFound;
        private readonly Action<AppState> _setState;
        private readonly Func<bool> _hasCameraPermission;
        public AppState State => AppState.QrScanning;

        public QrScanScreenPresenter(
            VisualTreeAsset asset,
            IQrScannerService qrScannerService,
            Action<string> onQrFound,
            Action<AppState> setState,
            Func<bool> hasCameraPermission)
        {
            _asset = asset;
            _qrScannerService = qrScannerService;
            _onQrFound = onQrFound;
            _setState = setState;
            _hasCameraPermission = hasCameraPermission;
        }

        public void Show(VisualElement container)
        {
            if (!_hasCameraPermission())
            {
                _setState(AppState.Permission);
                return;
            }

            var instance = _asset.Instantiate();
            instance.style.flexGrow = 1;
            container.Add(instance);
            ScreenBinders.WireQrScanner(container, _setState);
            _qrScannerService?.StartScanning(_onQrFound);
        }

        public void Hide()
        {
            _qrScannerService?.StopScanning();
        }
    }

    public sealed class NavigatingScreenPresenter : IScreenPresenter
    {
        private readonly VisualTreeAsset _asset;
        private readonly Action<AppState> _setState;
        private readonly Func<AppState> _getLastState;
        private readonly Action _onToggleVoice;
        private readonly Action _onOpenMap;
        private readonly Action _onEndNavigation;
        public AppState State => AppState.Navigating;
        public NavigatingScreenPresenter(VisualTreeAsset asset, Action<AppState> setState, Func<AppState> getLastState, Action onToggleVoice, Action onOpenMap, Action onEndNavigation)
        {
            _asset = asset; _setState = setState; _getLastState = getLastState; _onToggleVoice = onToggleVoice; _onOpenMap = onOpenMap; _onEndNavigation = onEndNavigation;
        }
        public void Show(VisualElement container)
        {
            var instance = _asset.Instantiate();
            instance.style.flexGrow = 1;
            container.Add(instance);
            ScreenBinders.WireArNavigation(container, _setState, _getLastState, _onToggleVoice, _onOpenMap, _onEndNavigation);
        }
    }

    public sealed class PositionLostScreenPresenter : IScreenPresenter
    {
        private readonly VisualTreeAsset _asset;
        private readonly Action<AppState> _setState;
        public AppState State => AppState.PositionLost;
        public PositionLostScreenPresenter(VisualTreeAsset asset, Action<AppState> setState) { _asset = asset; _setState = setState; }
        public void Show(VisualElement container)
        {
            var instance = _asset.Instantiate();
            instance.style.flexGrow = 1;
            container.Add(instance);
            ScreenBinders.WirePositionLost(container, _setState);
        }
    }

    public sealed class SettingsScreenPresenter : IScreenPresenter
    {
        private readonly VisualTreeAsset _asset;
        private readonly Action<AppState> _setState;
        private readonly Action _onSignOut;
        private readonly Action _onAbout;
        private readonly Action _onHelp;
        private readonly Action<ScreenBinders.SettingsState> _onSettingsChanged;
        public AppState State => AppState.Settings;
        public SettingsScreenPresenter(VisualTreeAsset asset, Action<AppState> setState, Action onSignOut, Action onAbout, Action onHelp, Action<ScreenBinders.SettingsState> onSettingsChanged)
        {
            _asset = asset; _setState = setState; _onSignOut = onSignOut; _onAbout = onAbout; _onHelp = onHelp; _onSettingsChanged = onSettingsChanged;
        }
        public void Show(VisualElement container)
        {
            var instance = _asset.Instantiate();
            instance.style.flexGrow = 1;
            container.Add(instance);
            ScreenBinders.WireSettings(container, _setState, _onSignOut, _onAbout, _onHelp, _onSettingsChanged);
        }
    }

    public sealed class FeedbackScreenPresenter : IScreenPresenter
    {
        private readonly VisualTreeAsset _asset;
        private readonly Action<AppState> _setState;
        private readonly Func<AppState> _getLastState;
        private readonly Action _onSubmit;
        public AppState State => AppState.Feedback;
        public FeedbackScreenPresenter(VisualTreeAsset asset, Action<AppState> setState, Func<AppState> getLastState, Action onSubmit)
        {
            _asset = asset; _setState = setState; _getLastState = getLastState; _onSubmit = onSubmit;
        }
        public void Show(VisualElement container)
        {
            var instance = _asset.Instantiate();
            instance.style.flexGrow = 1;
            container.Add(instance);
            ScreenBinders.WireFeedback(container, _setState, _getLastState, _onSubmit);
        }
    }

    public sealed class ComingSoonScreenPresenter : IScreenPresenter
    {
        private readonly VisualTreeAsset _asset;
        private readonly Action<AppState> _setState;
        private readonly Func<AppState> _getLastState;
        public AppState State => AppState.ComingSoon;
        public ComingSoonScreenPresenter(VisualTreeAsset asset, Action<AppState> setState, Func<AppState> getLastState)
        {
            _asset = asset; _setState = setState; _getLastState = getLastState;
        }
        public void Show(VisualElement container)
        {
            var instance = _asset.Instantiate();
            instance.style.flexGrow = 1;
            container.Add(instance);
            ScreenBinders.WireComingSoon(container, _setState, _getLastState);
        }
    }
}
