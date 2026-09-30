using UnityEngine;

namespace LowDefMustard.UIBox
{
    public interface IUIChoice
    {
        bool isAlive { get; } // Note: Destroyed MonoBehaviours behind an interface are not == null
        void Highlight(bool enable);
        void UseChoice();
        bool TryGetScreenRect(Camera renderCamera, out Rect screenRect);
    }
}
