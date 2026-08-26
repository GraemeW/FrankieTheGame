using UnityEditor;
using UnityEngine;

namespace LowDefMustard.Utils.Tests.Editor
{
    // Shared setup for tests that need a UI Toolkit panel to dispatch events through
    // No visible window pops up during the test run
    internal static class HeadlessEditorWindowTestHelper
    {
        public static EditorWindow CreateOffscreenWindow()
        {
            var window = ScriptableObject.CreateInstance<EditorWindow>();
            window.ShowUtility();
            window.position = new Rect(-10000, -10000, 200, 200);
            return window;
        }
    }
}
