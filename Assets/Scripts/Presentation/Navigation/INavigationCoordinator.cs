using NavAR.Core.Entities;

namespace NavAR.Presentation.Navigation
{
    public interface INavigationCoordinator
    {
        bool TrySelectBestEntrance(
            QRAnchor lastScannedAnchor,
            System.Collections.Generic.IReadOnlyList<Destination> entrances,
            out Destination selectedEntrance);
    }
}
