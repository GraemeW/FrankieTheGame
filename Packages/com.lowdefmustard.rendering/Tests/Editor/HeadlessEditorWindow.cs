using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Tests.Editor
{
    internal sealed class HeadlessEditorWindow
    {
        // Const Tunables
        private const float _offScreen = -10000f;
        
        // Data Structures
        private sealed class TestHostWindow : EditorWindow { }

        // Constructor
        public HeadlessEditorWindow(EditorWindow setWindow = null, float width = 600f, float height = 800f)
        {
            window = setWindow != null ? setWindow : ScriptableObject.CreateInstance<TestHostWindow>();
            window.position = new Rect(_offScreen, _offScreen, width, height);
            window.ShowPopup();
        }
        
        // Public Methods
        public EditorWindow window { get; }
        public VisualElement root => window.rootVisualElement;

        public void Close()
        {
            if (window != null) { window.Close(); }
        }

        // Yields editor updates until the condition holds (bindings + TrackPropertyValue are polled, not immediate)
        public static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds = 2f)
        {
            double timeout = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (!condition() && EditorApplication.timeSinceStartup < timeout) { yield return null; }
        }
    }
}
