#if UNITY_EDITOR
using UnityEditor;

namespace LowDefMustard.GameStateModifiers.Tests.Editor
{
    public sealed class TestHostWindow : EditorWindow
    {
        // Hosts an inspector root on a panel ~ req'd for ClickEvent dispatch to fire
    }
}
#endif
