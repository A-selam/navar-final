using UnityEngine;

namespace NavAR.Infrastructure.Navigation
{
    public sealed class SceneNavigationContextProvider : INavigationContextProvider
    {
        private NavigationSceneContext _current;

        public bool TryGetCurrent(out NavigationSceneContext context)
        {
            if (_current != null
                && _current.gameObject != null
                && _current.gameObject.scene.IsValid()
                && _current.gameObject.scene.isLoaded)
            {
                context = _current;
                return true;
            }

            context = null;
            return false;
        }

        public void Refresh()
        {
            _current = null;
            foreach (var candidate in Object.FindObjectsOfType<NavigationSceneContext>())
            {
                if (candidate == null || candidate.gameObject == null)
                {
                    continue;
                }

                var scene = candidate.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                _current = candidate;
                break;
            }
        }
    }
}
