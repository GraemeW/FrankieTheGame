using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Localization.Tests.Editor
{
    // Test Notes:
    //  - must attach element under test to editor window for CustomPropertyDrawer to be invoked
    //  - element under test must visible, or it will silently no-op on both .value assignment and SendEvent
    //      - thus, show window && move it far off-screen during test runs
    
    internal sealed class HeadlessEditorWindowTestHelper
    {
        private sealed class TestHeadlessEditorWindow : EditorWindow { }

        private readonly TestHeadlessEditorWindow window;

        public VisualElement root => window.rootVisualElement;

        public HeadlessEditorWindowTestHelper()
        {
            window = ScriptableObject.CreateInstance<TestHeadlessEditorWindow>();
            window.position = new Rect(-10000, -10000, 400, 400);
            window.ShowPopup();
        }

        public void Close()
        {
            if (window != null) { window.Close(); }
        }
    }
}
