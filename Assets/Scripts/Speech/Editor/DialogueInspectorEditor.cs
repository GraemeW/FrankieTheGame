using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Speech.Editor
{
    [CustomEditor(typeof(Dialogue))]
    public class DialogueInspectorEditor : UnityEditor.Editor
    {
        // Const UI
        private const float _rowHeight = 20f;
        private const float _lockToggleHeight = 20f;
        private const float _lockToggleLabelWidth = 20f;
        private const int _rowSpacingTop  = 2;
        private const int _rowSpacingBottom  = 2;
        private const string _lockLabel = "Editing Locked 🔒";
        private const string _unlockLabel = "Editing Unlocked 🔓";
        
        // State
        private bool isKeyUnlocked;
        private Toggle lockToggle;
        
        public override VisualElement CreateInspectorGUI()
        {
            isKeyUnlocked = false;
            
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var headerLabel = new Label("Extreme Danger Zone (careful!)");
            root.Add(headerLabel);
            
            VisualElement lockToggleRow = BuildLockToggleRow(isKeyUnlocked, out lockToggle); 
            root.Add(lockToggleRow);
            
            var regenerateGUIDs = new Button { text = "Regenerate GUIDs" };
            regenerateGUIDs.SetEnabled(isKeyUnlocked);
            regenerateGUIDs.RegisterCallback<ClickEvent>(RegenerateGUIDs);
            root.Add(regenerateGUIDs);
            
            var reserializeNodeDepthBreadth = new Button { text = "Reserialize Node Depth & Breadth" };
            reserializeNodeDepthBreadth.SetEnabled(isKeyUnlocked);
            reserializeNodeDepthBreadth.RegisterCallback<ClickEvent>(ReserializeNodeDepthBreadth);
            root.Add(reserializeNodeDepthBreadth);
            
            lockToggle.RegisterValueChangedCallback(evt =>
            {
                isKeyUnlocked = evt.newValue;
                Label toggleLabel = lockToggleRow.Q<Label>();
                if (toggleLabel != null) { toggleLabel.text = isKeyUnlocked ? _unlockLabel : _lockLabel; }
                
                regenerateGUIDs.SetEnabled(isKeyUnlocked);
                reserializeNodeDepthBreadth.SetEnabled(isKeyUnlocked);
            });

            return root;
        }

        private void RegenerateGUIDs(ClickEvent clickEvent)
        {
            var dialogue = (Dialogue)target;
            if (dialogue == null)
            {
                return;
            }

            dialogue.RegenerateGUIDs();
        }

        private void ReserializeNodeDepthBreadth(ClickEvent clickEvent)
        {
            var dialogue = (Dialogue)target;
            if (dialogue == null)
            {
                return;
            }

            dialogue.ReserializeNodeDepthBreadth();
        }
        
        private static VisualElement BuildLockToggleRow(bool isKeyUnlocked, out Toggle lockToggle)
        {
            VisualElement lockToggleRow = MakeLockToggleBaseRow();
            lockToggle = MakeToggle(isKeyUnlocked);
            lockToggleRow.Add(lockToggle);
            return lockToggleRow;
        }
        
        private static VisualElement MakeLockToggleBaseRow()
        {
            var lockToggleBaseRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginTop = _rowSpacingTop,
                    marginBottom = _rowSpacingBottom,
                    height = _rowHeight,
                }
            };
            return lockToggleBaseRow;
        }
        
        private static Toggle MakeToggle(bool isUnlocked)
        {
            return new Toggle
            {
                label = isUnlocked ? _unlockLabel : _lockLabel,
                labelElement = { 
                    style =
                    {
                        minWidth = _lockToggleLabelWidth,
                        unityTextAlign = TextAnchor.MiddleLeft
                    } 
                },
                value = isUnlocked,
                style = { height = _lockToggleHeight }
            };
        }
    }
}
