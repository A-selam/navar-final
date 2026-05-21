using System.Collections;
using NavAR.Core;
using NavAR.Core.State;
using NavAR.Core.Navigation;
using NavAR.Infrastructure.Navigation;
using UnityEngine;

namespace NavAR.Presentation.Navigation
{
    public sealed class NavigationTransitionSequencer
    {
        private readonly MonoBehaviour _runner;
        private readonly AppStateManager _stateManager;
        private readonly System.Func<HybridGraphPathCalculator> _getHybridCalculator;
        private readonly float _transitionArrivalRadiusMeters;
        private readonly System.Action<int, string, string> _beginFloorTransition;
        private readonly System.Action<Vector3> _setPendingTransitionLanding;
        private readonly System.Action<string> _log;

        private Coroutine _watchRoutine;

        public NavigationTransitionSequencer(
            MonoBehaviour runner,
            AppStateManager stateManager,
            System.Func<HybridGraphPathCalculator> getHybridCalculator,
            float transitionArrivalRadiusMeters,
            System.Action<int, string, string> beginFloorTransition,
            System.Action<Vector3> setPendingTransitionLanding,
            System.Action<string> log)
        {
            _runner = runner;
            _stateManager = stateManager;
            _getHybridCalculator = getHybridCalculator;
            _transitionArrivalRadiusMeters = transitionArrivalRadiusMeters;
            _beginFloorTransition = beginFloorTransition;
            _setPendingTransitionLanding = setPendingTransitionLanding;
            _log = log;
        }

        public void StartIfNeeded()
        {
            var hybridCalculator = _getHybridCalculator?.Invoke();
            if (hybridCalculator == null)
            {
                return;
            }

            if (!hybridCalculator.TryGetPendingTransition(
                    out var targetFloorId,
                    out var targetFloorLabel,
                    out var transitionNodeId,
                    out var transitionNodePosition,
                    out var transitionLandingPosition))
            {
                return;
            }

            _setPendingTransitionLanding?.Invoke(transitionLandingPosition);
            Stop();
            _watchRoutine = _runner.StartCoroutine(
                WatchForArrivalAtTransitionNode(targetFloorId, targetFloorLabel, transitionNodeId, transitionNodePosition));
            _log?.Invoke($"UIManager: Watching for arrival at transition node {transitionNodeId} to prompt floor {targetFloorId}.");
        }

        public void Stop()
        {
            if (_watchRoutine != null)
            {
                _runner.StopCoroutine(_watchRoutine);
                _watchRoutine = null;
            }
        }

        private IEnumerator WatchForArrivalAtTransitionNode(int targetFloorId, string targetFloorLabel, string transitionNodeId, Vector3 transitionNodePosition)
        {
            while (_stateManager != null && _stateManager.CurrentState == AppState.Navigating)
            {
                var cameraTransform = Camera.main != null ? Camera.main.transform : null;
                if (cameraTransform == null)
                {
                    yield return null;
                    continue;
                }

                var currentPos = cameraTransform.position;
                var horizontalDistance = Vector2.Distance(
                    new Vector2(currentPos.x, currentPos.z),
                    new Vector2(transitionNodePosition.x, transitionNodePosition.z));

                if (horizontalDistance <= _transitionArrivalRadiusMeters)
                {
                    _log?.Invoke($"UIManager: Reached transition node {transitionNodeId} (distance={horizontalDistance:F2}m). Prompting floor transition.");
                    _watchRoutine = null;
                    _beginFloorTransition?.Invoke(targetFloorId, targetFloorLabel, transitionNodeId);
                    yield break;
                }

                yield return null;
            }

            _watchRoutine = null;
        }
    }
}
