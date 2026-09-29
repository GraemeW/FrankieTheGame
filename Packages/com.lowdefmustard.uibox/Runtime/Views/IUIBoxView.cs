using System;

namespace LowDefMustard.UIBox
{
    // If no IUIBoxView component is present, UIBoxes fall back to the legacy uGUI implementation
    public interface IUIBoxView
    {
        void SetVisible(bool enable);
        void SetBackExitAction(Action onBackExit); // Back-exit affordance is hidden until an action is set
    }
}
