using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Saving.Tests.Editor
{
    // Attached panel so that value-changed/click event dispatch fires
    // Caller is responsible for disposing via Close() in [TearDown]
    public class HeadlessEditorWindowTestHelper
    {
        private readonly EditorWindow window;

        public HeadlessEditorWindowTestHelper()
        {
            window = ScriptableObject.CreateInstance<EditorWindow>();
            window.ShowUtility();
            window.position = new Rect(-10000, -10000, 200, 200);
        }

        public void Attach(VisualElement element) => window.rootVisualElement.Add(element);

        public void Close()
        {
            if (window != null) { window.Close(); }
        }
    }
}
