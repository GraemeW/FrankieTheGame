using UnityEngine;

namespace LowDefMustard.UIBox
{
    public interface IUIChoice
    {
        bool isAlive { get; } // Note: Destroyed MonoBehaviours behind an interface are not == null
        void Highlight(bool enable);
        void UseChoice();
        void SetText(string text);
        bool TryGetNavigationRect(out Rect navigationRect); // For spatial cursor movement
    }
}
