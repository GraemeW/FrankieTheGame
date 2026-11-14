using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Unity.GraphToolkit.Editor;

namespace Frankie.Speech.Editor
{
    internal static class DialogueVariableFieldHook
    {
        // Note:
        //  - Graph Toolkit has no public hook for port fields && thus does not allow custom picker out of box
        //  - Thus, instead we are intercepting the field's picker via UITK class and then swapping it for DialogueVariablePicker
        //      - Alt + click keeps Unity's own picker as back-up option
        //      - Matches ScriptableObject ports named as {n} variable ports only (other ScriptableObject ports would keep Unity's picker)
        //  - Internal methods/properties are for test visibility

        // Const Tunables
        internal const string selectorClass = "unity-object-field__selector";
        internal const string variablePortClass = "ge-port--data-type-scriptable-object";
        private const string _portModelProperty = "PortModel";

        #region PublicMethods
        public static void Register(VisualElement windowRoot)
        {
            windowRoot.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            windowRoot.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            windowRoot.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            windowRoot.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
        }

        public static bool TryGetVariableField(VisualElement target, out ObjectField field)
        {
            field = null;
            VisualElement selector = FindSelfOrAncestor(target, element => element.ClassListContains(selectorClass));
            if (selector == null) { return false; }

            field = FindSelfOrAncestor(selector, element => element is ObjectField) as ObjectField;
            if (field == null) { return false; }

            VisualElement port = FindSelfOrAncestor(field, element => element.ClassListContains(variablePortClass));
            return port != null && IsVariablePort(port);
        }
        #endregion

        #region PrivateMethods
        private static void OnPointerDown(PointerDownEvent pointerDownEvent)
        {
            if (pointerDownEvent.button != 0 || pointerDownEvent.altKey || !TryGetVariableField(pointerDownEvent.target as VisualElement, out ObjectField field)) { return; }
            pointerDownEvent.StopImmediatePropagation();
            DialogueVariablePicker.Show(field.worldBound, variable => field.value = variable);
        }

        private static void OnMouseDown(MouseDownEvent mouseDownEvent)
        {
            // Blocks the compatibility mouse event too, so Unity's picker doesn't open alongside ours
            if (mouseDownEvent.button != 0 || mouseDownEvent.altKey || !TryGetVariableField(mouseDownEvent.target as VisualElement, out _)) { return; }
            mouseDownEvent.StopImmediatePropagation();
        }

        private static bool IsVariablePort(VisualElement port)
        {
            // Port view type is internal ~ its model is read by property name, then through the public IPort (fails closed:  no match keeps Unity's picker)
            return port.GetType().GetProperty(_portModelProperty)?.GetValue(port) is IPort portModel && SpeechGraphNode.IsVariablePortName(portModel.Name);
        }

        private static VisualElement FindSelfOrAncestor(VisualElement element, Func<VisualElement, bool> predicate)
        {
            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (predicate(current)) { return current; }
            }
            return null;
        }
        #endregion
    }
}
