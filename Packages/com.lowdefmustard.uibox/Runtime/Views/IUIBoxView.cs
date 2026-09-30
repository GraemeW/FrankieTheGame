using System;

namespace LowDefMustard.UIBox
{
    // If no IUIBoxView component is present, UIBoxes fall back to the legacy uGUI implementation
    public interface IUIBoxView
    {
        void SetVisible(bool enable);
        void SetBackExitAction(Action onBackExit); // Back-exit affordance is hidden until an action is set
        IUIChoice CreateChoiceOption(string text, int choiceOrder, Action onChoose); // Fixed choices, shown immediately (i.e. unlike text-scan generate-on-node choice entries)
    }
}
