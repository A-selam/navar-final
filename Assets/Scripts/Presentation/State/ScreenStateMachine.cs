using System;
using System.Collections.Generic;
using NavAR.Core.State;

namespace NavAR.Presentation.State
{
    public sealed class ScreenStateMachine
    {
        private readonly Dictionary<AppState, Action> _handlers = new Dictionary<AppState, Action>();
        private readonly Dictionary<AppState, HashSet<AppState>> _allowedTransitions = new Dictionary<AppState, HashSet<AppState>>();
        private Action _fallbackHandler;
        private Func<AppState, AppState, bool> _guard;

        public void Register(AppState state, Action handler)
        {
            _handlers[state] = handler;
        }

        public void RegisterFallback(Action handler)
        {
            _fallbackHandler = handler;
        }

        public void RegisterAllowedTransition(AppState from, params AppState[] toStates)
        {
            if (!_allowedTransitions.TryGetValue(from, out var targets))
            {
                targets = new HashSet<AppState>();
                _allowedTransitions[from] = targets;
            }

            foreach (var to in toStates)
            {
                targets.Add(Normalize(to));
            }
        }

        public void SetTransitionGuard(Func<AppState, AppState, bool> guard)
        {
            _guard = guard;
        }

        public bool TryTransition(AppState current, AppState requested, out AppState resolved)
        {
            current = Normalize(current);
            requested = Normalize(requested);
            resolved = requested;

            if (current == requested)
            {
                return true;
            }

            if (_guard != null && !_guard(current, requested))
            {
                return false;
            }

            if (!_allowedTransitions.TryGetValue(current, out var targets))
            {
                return false;
            }

            return targets.Contains(requested);
        }

        public void Execute(AppState state)
        {
            state = Normalize(state);
            if (_handlers.TryGetValue(state, out var handler))
            {
                handler?.Invoke();
                return;
            }

            _fallbackHandler?.Invoke();
        }

        private static AppState Normalize(AppState state)
        {
            if (state == AppState.DestinationSelection)
            {
                return AppState.Explore;
            }

            return state;
        }
    }
}
