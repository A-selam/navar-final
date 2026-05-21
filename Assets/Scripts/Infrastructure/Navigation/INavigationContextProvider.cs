namespace NavAR.Infrastructure.Navigation
{
    public interface INavigationContextProvider
    {
        bool TryGetCurrent(out NavigationSceneContext context);
        void Refresh();
    }
}
