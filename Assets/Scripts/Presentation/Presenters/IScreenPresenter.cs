using NavAR.Core.State;
using UnityEngine.UIElements;

namespace NavAR.Presentation.Presenters
{
    public interface IScreenPresenter
    {
        AppState State { get; }
        void Show(VisualElement container);
    }

    public interface IHideablePresenter
    {
        void Hide();
    }
}
