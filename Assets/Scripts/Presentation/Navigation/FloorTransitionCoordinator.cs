using System;
using System.Collections;
using System.Collections.Generic;
using NavAR.Core.Entities;
using NavAR.Core.State;
using NavAR.Core.Interfaces;
using NavAR.Infrastructure;
using NavAR.Infrastructure.Navigation;
using UnityEngine;

namespace NavAR.Presentation.Navigation
{
    public sealed class FloorTransitionCoordinator
    {
        private readonly AppStateManager _stateManager;
        private readonly IFloorSceneTransitionService _floorTransitionService;
        private readonly Func<bool> _ensureNavigationServices;
        private readonly Func<Vector3, Vector3, int, int?, IReadOnlyList<string>, List<Vector3>> _calculatePathForCurrentFloor;
        private readonly Action<List<Vector3>, bool> _drawPathAsync;
        private readonly NavigationSequencer _navigationSequencer;
        private readonly Action _resetNavigationServiceReferences;
        private readonly Action _snapXrOriginToPendingTransitionLanding;
        private readonly Func<NavigationSceneContext> _getNavigationSceneContext;
        private readonly Action<string> _log;
        private readonly Action<string> _logError;
        private readonly Action<string> _logWarning;

        public FloorTransitionCoordinator(
            AppStateManager stateManager,
            IFloorSceneTransitionService floorTransitionService,
            Func<bool> ensureNavigationServices,
            Func<Vector3, Vector3, int, int?, IReadOnlyList<string>, List<Vector3>> calculatePathForCurrentFloor,
            Action<List<Vector3>, bool> drawPathAsync,
            NavigationSequencer navigationSequencer,
            Action resetNavigationServiceReferences,
            Action snapXrOriginToPendingTransitionLanding,
            Func<NavigationSceneContext> getNavigationSceneContext,
            Action<string> log,
            Action<string> logError,
            Action<string> logWarning)
        {
            _stateManager = stateManager;
            _floorTransitionService = floorTransitionService;
            _ensureNavigationServices = ensureNavigationServices;
            _calculatePathForCurrentFloor = calculatePathForCurrentFloor;
            _drawPathAsync = drawPathAsync;
            _navigationSequencer = navigationSequencer;
            _resetNavigationServiceReferences = resetNavigationServiceReferences;
            _snapXrOriginToPendingTransitionLanding = snapXrOriginToPendingTransitionLanding;
            _getNavigationSceneContext = getNavigationSceneContext;
            _log = log;
            _logError = logError;
            _logWarning = logWarning;
        }

        public void BeginFloorTransition(int targetFloorId, string targetFloorLabel = null, string transitionNodeId = null)
        {
            if (_stateManager == null)
            {
                _logError?.Invoke("UIManager: Cannot begin floor transition because the state manager is not initialized.");
                return;
            }

            _stateManager.Context.PendingFloorId = targetFloorId;
            _stateManager.Context.PendingFloorLabel = targetFloorLabel;
            _stateManager.Context.PendingTransitionNodeId = transitionNodeId;
            _stateManager.ChangeState(AppState.FloorTransition);
        }

        public void ConfirmFloorTransition()
        {
            if (_stateManager == null)
            {
                return;
            }

            var destination = _stateManager.Context.CurrentDestination;
            var targetFloorId = _stateManager.Context.PendingFloorId;

            if (targetFloorId > 0)
            {
                _stateManager.Context.CurrentFloorId = targetFloorId;
            }

            _stateManager.Context.PendingFloorId = 0;
            _stateManager.Context.PendingFloorLabel = null;
            _stateManager.Context.PendingTransitionNodeId = null;
            _stateManager.ChangeState(AppState.Navigating);

            if (destination == null)
            {
                return;
            }

            if (_floorTransitionService != null && targetFloorId > 0)
            {
                _log?.Invoke($"[UIManager] Requesting floor transition to floor {targetFloorId}...");
                _floorTransitionService.RequestFloorTransition(targetFloorId);
            }

            _navigationSequencer.StartFloorContinuation(ResumeNavigationAfterFloorTransition(destination, targetFloorId));
        }

        private IEnumerator ResumeNavigationAfterFloorTransition(Destination destination, int targetFloorId)
        {
            _resetNavigationServiceReferences?.Invoke();
            var attempt = 0;
            while (true)
            {
                attempt++;

                if (_floorTransitionService != null && targetFloorId > 0)
                {
                    if (_floorTransitionService.IsTransitionInProgress)
                    {
                        if (attempt == 1 || attempt % 120 == 0)
                        {
                            _log?.Invoke($"[UIManager] Waiting for floor {targetFloorId} to load... (attempt {attempt})");
                        }
                        yield return null;
                        continue;
                    }
                }

                if (_ensureNavigationServices())
                {
                    var navContext = _getNavigationSceneContext?.Invoke();
                    if (navContext == null
                        || navContext.gameObject == null
                        || !navContext.gameObject.scene.IsValid()
                        || !navContext.gameObject.scene.isLoaded)
                    {
                        if (attempt == 1 || attempt % 120 == 0)
                        {
                            _log?.Invoke($"[UIManager] Waiting for NavigationSceneContext to be ready after floor transition... (attempt {attempt})");
                        }
                        yield return null;
                        continue;
                    }

                    _log?.Invoke($"[UIManager] NavigationSceneContext ready in scene '{navContext.gameObject.scene.name}' after transition to floor {targetFloorId}.");

                    _snapXrOriginToPendingTransitionLanding?.Invoke();

                    var currentCamera = Camera.main != null ? Camera.main.transform : null;
                    var startPos = currentCamera != null
                        ? currentCamera.position
                        : Vector3.zero;
                    var targetPos = startPos;
                    var continuationPath = _calculatePathForCurrentFloor(startPos, targetPos, _stateManager.Context.CurrentFloorId, destination.floor_id, destination.entrance_node_ids);

                    if (continuationPath != null && continuationPath.Count > 0)
                    {
                        _drawPathAsync(continuationPath, false);
                    }
                    else
                    {
                        _logWarning?.Invoke("[UIManager] Could not calculate continuation path after floor transition confirmation.");
                    }

                    yield break;
                }

                yield return null;
            }
        }
    }
}
