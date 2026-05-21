using System.Collections;
using UnityEngine;

namespace NavAR.Presentation.Navigation
{
    public sealed class NavigationSequencer
    {
        private readonly MonoBehaviour _runner;
        private Coroutine _qrFlowRoutine;
        private Coroutine _floorContinuationRoutine;

        public NavigationSequencer(MonoBehaviour runner)
        {
            _runner = runner;
        }

        public void StartQrFlow(IEnumerator routine)
        {
            CancelQrFlow();
            _qrFlowRoutine = _runner.StartCoroutine(WrapQr(routine));
        }

        public void StartFloorContinuation(IEnumerator routine)
        {
            CancelFloorContinuation();
            _floorContinuationRoutine = _runner.StartCoroutine(WrapFloor(routine));
        }

        public void CancelQrFlow()
        {
            if (_qrFlowRoutine != null)
            {
                _runner.StopCoroutine(_qrFlowRoutine);
                _qrFlowRoutine = null;
            }
        }

        public void CancelFloorContinuation()
        {
            if (_floorContinuationRoutine != null)
            {
                _runner.StopCoroutine(_floorContinuationRoutine);
                _floorContinuationRoutine = null;
            }
        }

        public void CancelAll()
        {
            CancelQrFlow();
            CancelFloorContinuation();
        }

        private IEnumerator WrapQr(IEnumerator routine)
        {
            yield return routine;
            _qrFlowRoutine = null;
        }

        private IEnumerator WrapFloor(IEnumerator routine)
        {
            yield return routine;
            _floorContinuationRoutine = null;
        }
    }
}
