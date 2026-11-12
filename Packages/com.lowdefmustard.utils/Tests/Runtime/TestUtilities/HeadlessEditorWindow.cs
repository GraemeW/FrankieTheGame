#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Utils.Tests
{
    public static class HeadlessEditorWindow
    {
        // Note:
        //  - ensure to yield WaitUntilSettled before the first event (panel + layout land over later editor updates)
        //  - on mouse gestures (e.g. down -> drag -> up):
        //      - whole gesture occurs w/in one tick
        //      - capture is global editor state between ticks
        
        // Const Tunables
        private const float _offScreen = -10000f;
        private const int _settleTicks = 2;

        #region PublicMethods
        public static EditorWindow CreateOffscreenWindow(float width = 200f, float height = 200f) => ShowOffscreen(ScriptableObject.CreateInstance<EditorWindow>(), width, height);
        
        public static T ShowOffscreen<T>(T window, float width = 200f, float height = 200f) where T : EditorWindow
        {
            window.ShowUtility();
            window.position = new Rect(_offScreen, _offScreen, width, height);
            return window;
        }

        public static IEnumerator WaitUntilSettled(EditorWindow window, float timeoutSeconds = 2f)
        {
            yield return TestWait.Until(() => IsReady(window), timeoutSeconds);
            Assert.IsTrue(IsReady(window), "Headless window never attached to a panel with a resolved layout");
            yield return Settle();
        }
        
        public static IEnumerator Settle()
        {
            for (int i = 0; i < _settleTicks; i++) { yield return null; }
        }

        public static void Close(EditorWindow window)
        {
            if (window == null) { return; }

            VisualElement capturing = window.rootVisualElement.panel?.GetCapturingElement(PointerId.mousePointerId) as VisualElement;
            capturing?.ReleaseMouse();
            window.Close();
        }

        public static void SendMouseDown(VisualElement target, Vector2 position, int button = 0, bool altKey = false, int clickCount = 1)
        {
            using MouseDownEvent mouseDownEvent = MouseDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = position, button = button, clickCount = clickCount, modifiers = altKey ? EventModifiers.Alt : EventModifiers.None });
            mouseDownEvent.target = target;
            target.SendEvent(mouseDownEvent);
        }

        public static void SendMouseDrag(VisualElement target, Vector2 position, int button = 0) => SendMouseDrag(target, position, Vector2.zero, button);

        public static void SendMouseDrag(VisualElement target, Vector2 position, Vector2 delta, int button = 0)
        {
            using MouseMoveEvent mouseMoveEvent = MouseMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, mousePosition = position, delta = delta, button = button });
            mouseMoveEvent.target = target;
            target.SendEvent(mouseMoveEvent);
        }

        public static void SendMouseUp(VisualElement target, Vector2 position, int button = 0)
        {
            using MouseUpEvent mouseUpEvent = MouseUpEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = position, button = button });
            mouseUpEvent.target = target;
            target.SendEvent(mouseUpEvent);
        }

        public static void SendClick(VisualElement target, int clickCount = 1)
        {
            using ClickEvent clickEvent = ClickEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, clickCount = clickCount });
            clickEvent.target = target;
            target.SendEvent(clickEvent);
        }
        #endregion

        #region PrivateMethods
        private static bool IsReady(EditorWindow window)
        {
            if (window == null) { return false; }

            VisualElement root = window.rootVisualElement;
            return root.panel != null && root.layout.width > 0f && !float.IsNaN(root.layout.width);
        }
        #endregion
    }
}
#endif
