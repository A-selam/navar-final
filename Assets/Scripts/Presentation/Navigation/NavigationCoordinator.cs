using System;
using System.Collections.Generic;
using System.Linq;
using NavAR.Core.Entities;
using NavAR.Core.Interfaces;

namespace NavAR.Presentation.Navigation
{
    public sealed class NavigationCoordinator : INavigationCoordinator
    {
        private readonly IEntranceSelector _entranceSelector;

        public NavigationCoordinator(IEntranceSelector entranceSelector)
        {
            _entranceSelector = entranceSelector;
        }

        public bool TrySelectBestEntrance(QRAnchor lastScannedAnchor, IReadOnlyList<Destination> entrances, out Destination selectedEntrance)
        {
            selectedEntrance = null;
            if (entrances == null || entrances.Count == 0)
            {
                return false;
            }

            if (_entranceSelector != null)
            {
                try
                {
                    selectedEntrance = _entranceSelector.SelectBestEntrance(lastScannedAnchor, entrances.ToList());
                }
                catch (Exception)
                {
                    selectedEntrance = null;
                }
            }

            selectedEntrance ??= entrances[0];
            return selectedEntrance != null;
        }
    }
}
