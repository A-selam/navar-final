using System.Collections.Generic;
using NavAR.Core.Entities;
using NavAR.Core.Interfaces;

namespace NavAR.Infrastructure.Navigation
{
    public class NavMeshEntranceSelector : IEntranceSelector
    {
        private readonly IPathCalculator _pathCalculator;

        public NavMeshEntranceSelector(IPathCalculator pathCalculator)
        {
            _pathCalculator = pathCalculator;
        }

        public Destination SelectBestEntrance(QRAnchor startAnchor, List<Destination> entrances)
        {
            if (entrances == null || entrances.Count == 0)
            {
                return null;
            }

            return entrances[0];
        }
    }
}
