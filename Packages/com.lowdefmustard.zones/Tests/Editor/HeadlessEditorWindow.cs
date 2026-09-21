using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Zones.Tests.Editor
{
    // Shared setup for tests that need a UI Toolkit panel to dispatch events
    internal static class HeadlessEditorWindow
    {
        public static EditorWindow CreateOffscreenWindow()
        {
            var window = ScriptableObject.CreateInstance<EditorWindow>();
            window.ShowUtility();
            window.position = new Rect(-10000, -10000, 200, 200);
            return window;
        }
        
        public static void SendMouseDown(VisualElement target, Vector2 position, int button = 0, bool altKey = false, int clickCount = 1)
        {
            using MouseDownEvent evt = MouseDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = position, button = button, clickCount = clickCount, modifiers = altKey ? EventModifiers.Alt : EventModifiers.None });
            evt.target = target;
            target.SendEvent(evt);
        }

        public static void SendMouseDrag(VisualElement target, Vector2 position, int button = 0)
        {
            using MouseMoveEvent evt = MouseMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, mousePosition = position, button = button });
            evt.target = target;
            target.SendEvent(evt);
        }
        
        public static void SendMouseDrag(VisualElement target, Vector2 position, Vector2 delta)
        {
            using MouseMoveEvent evt = MouseMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, mousePosition = position, delta = delta, button = 0 });
            evt.target = target;
            target.SendEvent(evt);
        }

        public static void SendMouseUp(VisualElement target, Vector2 position, int button = 0)
        {
            using MouseUpEvent evt = MouseUpEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = position, button = button });
            evt.target = target;
            target.SendEvent(evt);
        }
        
        public static void SendClick(VisualElement element)
        {
            using ClickEvent clickEvent = ClickEvent.GetPooled();
            clickEvent.target = element;
            element.SendEvent(clickEvent);
        }
    }
}
