using System;
using System.Collections;
using System.Collections.Generic;
using NavAR.Core.Entities;
using NavAR.Core.Interfaces;
using NavAR.Core.State;
using NavAR.Infrastructure;
using NavAR.Infrastructure.Navigation;
using UnityEngine;

namespace NavAR.Presentation.Navigation
{
    public sealed class QrNavigationCoordinator
    {
        private readonly AppStateManager _stateManager;
        private readonly Func<string, QRAnchor> _resolveAnchor;
        private readonly Func<AlignmentService> _getAlignmentService;
        private readonly IFloorSceneTransitionService _floorTransitionService;
        private readonly Action _resetNavigationServiceReferences;
        private readonly Func<bool> _ensureNavigationServices;
        private readonly Func<Vector3, Vector3, int, int?, IReadOnlyList<string>, List<Vector3>> _calculatePathForCurrentFloor;
        private readonly Action<List<Vector3>, bool> _drawPathAsync;
        private readonly NavigationSequencer _navigationSequencer;
        private readonly Action<string> _log;
        private readonly Action<string> _logError;

        public QrNavigationCoordinator(
            AppStateManager stateManager,
            Func<string, QRAnchor> resolveAnchor,
            Func<AlignmentService> getAlignmentService,
            IFloorSceneTransitionService floorTransitionService,
            Action resetNavigationServiceReferences,
            Func<bool> ensureNavigationServices,
            Func<Vector3, Vector3, int, int?, IReadOnlyList<string>, List<Vector3>> calculatePathForCurrentFloor,
            Action<List<Vector3>, bool> drawPathAsync,
            NavigationSequencer navigationSequencer,
            Action<string> log,
            Action<string> logError)
        {
            _stateManager = stateManager;
            _resolveAnchor = resolveAnchor;
            _getAlignmentService = getAlignmentService;
            _floorTransitionService = floorTransitionService;
            _resetNavigationServiceReferences = resetNavigationServiceReferences;
            _ensureNavigationServices = ensureNavigationServices;
            _calculatePathForCurrentFloor = calculatePathForCurrentFloor;
            _drawPathAsync = drawPathAsync;
            _navigationSequencer = navigationSequencer;
            _log = log;
            _logError = logError;
        }

        public void OnDestinationSelected(Destination destination)
        {
            if (destination == null)
            {
                _logError?.Invoke("[UIManager] Destination is null!");
                return;
            }

            _log?.Invoke($"[UIManager] Destination selected: {destination.name}. Transitioning to QR scanning to establish start position.");
            _stateManager.Context.CurrentDestination = destination;
            _stateManager.ChangeState(AppState.QrScanning);
        }

        public void OnQrCodeFound(string qrPayload)
        {
            var anchor = _resolveAnchor?.Invoke(qrPayload);
            if (anchor == null)
            {
                _logError?.Invoke($"[UIManager] QR {qrPayload} not found in database!");
                return;
            }

            _navigationSequencer.StartQrFlow(HandleQrScanWithAlignment(anchor, qrPayload));
        }

        private IEnumerator HandleQrScanWithAlignment(QRAnchor anchor, string qrPayload)
        {
            _resetNavigationServiceReferences?.Invoke();
            _stateManager.Context.LastScannedAnchor = anchor;
            _stateManager.Context.CurrentFloorId = anchor.floor_id;
            var didRealign = false;

            if (_floorTransitionService != null)
            {
                _log?.Invoke($"[UIManager] Loading floor scene for floor {anchor.floor_id} in background...");
                _floorTransitionService.RequestFloorTransition(anchor.floor_id);
            }

            // Alignment must come from the floor scene NavigationSceneContext.
            // Wait for floor load + context-bound services, then realign.
            const int maxAlignmentAttempts = 120;
            for (var attempt = 0; attempt < maxAlignmentAttempts; attempt++)
            {
                if (_floorTransitionService != null && _floorTransitionService.IsTransitionInProgress)
                {
                    yield return null;
                    continue;
                }

                _ensureNavigationServices?.Invoke();
                var alignment = _getAlignmentService?.Invoke();
                if (alignment != null)
                {
                    _log?.Invoke($"[UIManager] QR {qrPayload} found. Performing spatial alignment...");
                    alignment.Realign(anchor);
                    didRealign = true;
                    break;
                }

                yield return null;
            }

            if (!didRealign)
            {
                _logError?.Invoke("[UIManager] QR alignment failed or service unavailable after waiting. Returning to QR scan so user can retry recenter.");
                _stateManager.ChangeState(AppState.PositionLost);
                yield break;
            }

            var destination = _stateManager.Context.CurrentDestination;
            if (destination == null)
            {
                _logError?.Invoke("[UIManager] QR scanned but no destination was selected!");
                yield break;
            }

            yield return StartNavigationAfterQrScan(anchor, destination);
        }

        private IEnumerator StartNavigationAfterQrScan(QRAnchor startAnchor, Destination destination)
        {
            const int maxAttempts = 30;

            if (_floorTransitionService != null)
            {
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    if (_floorTransitionService.IsTransitionInProgress)
                    {
                        _log?.Invoke($"[UIManager] Waiting for floor scene to load... (attempt {attempt + 1}/{maxAttempts})");
                        yield return null;
                        continue;
                    }

                    break;
                }
            }

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (_ensureNavigationServices())
                {
                    break;
                }

                yield return null;
            }

            var startPos = new Vector3(startAnchor.x, startAnchor.y, startAnchor.z);
            var targetPos = startPos;
            _log?.Invoke($"[UIManager] Calculating path from QR position for floor {_stateManager.Context.CurrentFloorId} to destination on floor {destination.floor_id}.");
            var path = _calculatePathForCurrentFloor(startPos, targetPos, _stateManager.Context.CurrentFloorId, destination.floor_id, destination.entrance_node_ids);
            _log?.Invoke($"[UIManager] Path calculation returned {path?.Count ?? 0} corners.");

            if (path != null && path.Count > 0)
            {
                _log?.Invoke($"[UIManager] Drawing path with {path.Count} corners.");
                _drawPathAsync(path, true);
            }
            else
            {
                _logError?.Invoke("[UIManager] Failed to calculate path after QR scan!");
                _stateManager.ChangeState(AppState.Explore);
            }
        }
    }
}
