using System;

namespace LowDefMustard.UIBox
{
    public interface IUIBoxView
    {
        void SetVisible(bool enable);
        void SetPointerInputEnabled(bool enable); // Mirrors the box's active input (e.g. no clicks while a child box is on top)
        void SetDataSource(object dataSource); // Root data source for model-bound elements (e.g. headers)
        void SetBackExitAction(Action onBackExit); // Back-exit affordance is hidden until an action is set
        IUIChoice CreateChoiceOption(string text, int choiceOrder, Action onChoose, Action onHighlight = null); // Fixed choices, shown immediately (i.e. unlike text-scan generate-on-node choice entries)
        void CreateChoiceSeparator(ChoiceSeparatorType separatorType); // Divider between the choices created before and after it
    }
}
