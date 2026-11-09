using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using LowDefMustard.Zones;
using Frankie.Core;

namespace Frankie.Utils.Editor
{
    [CustomEditor(typeof(FrankieDebugger))]
    public sealed class FrankieDebuggerEditor : UnityEditor.Editor
    {
        // Const Tunables
        private const long _refreshIntervalMilliseconds = 250;
        private const string _noZoneNode = "(none)";
        
        // Property Refs
        private static readonly string[] _playModeDebugFields =
        {
            FrankieDebugger.debugEnemiesField,
            FrankieDebugger.debugCombatTransitionTypeField,
            FrankieDebugger.debugZoneField,
            FrankieDebugger.debugZoneNodeIDField,
            FrankieDebugger.debugStatusEffectStatField,
            FrankieDebugger.debugStatusEffectAmountField,
            FrankieDebugger.debugStatusEffectDurationField
        };

        // State
        private readonly List<VisualElement> playModeOnlyElements = new();
        private HelpBox playModeHelpBox;
        private Label playerStateLabel;
        private PopupField<string> zoneNodeField;

        #region UnityMethods
        public override VisualElement CreateInspectorGUI()
        {
            playModeOnlyElements.Clear();
            var root = new VisualElement();

            SerializedProperty iterator = serializedObject.GetIterator();
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                if (Array.IndexOf(_playModeDebugFields, iterator.name) >= 0) { continue; }
                var propertyField = new PropertyField(iterator.Copy());
                if (iterator.propertyPath == "m_Script") { propertyField.SetEnabled(false); }
                root.Add(propertyField);
            }

            root.Add(BuildPlayModeDebug());

            // Polled:  inspector has no event for play mode / player state changes on the live debugger (editor-only, cheap)
            RefreshPlayModeState();
            root.schedule.Execute(RefreshPlayModeState).Every(_refreshIntervalMilliseconds);
            return root;
        }
        #endregion

        #region PlayModeState
        private bool IsLiveDebugger(out FrankieDebugger debugger)
        {
            debugger = target as FrankieDebugger;
            return EditorApplication.isPlaying && debugger != null && !EditorUtility.IsPersistent(debugger);
        }

        private void RefreshPlayModeState()
        {
            bool isLive = IsLiveDebugger(out FrankieDebugger debugger);
            playModeHelpBox.style.display = isLive ? DisplayStyle.None : DisplayStyle.Flex;
            foreach (VisualElement element in playModeOnlyElements) { element.SetEnabled(isLive); }

            playerStateLabel.style.display = isLive ? DisplayStyle.Flex : DisplayStyle.None;
            if (!isLive) { return; }
            PlayerStateType? playerStateType = FrankieDebugger.GetPlayerStateType();
            playerStateLabel.text = $"Player state: {(playerStateType.HasValue ? playerStateType.Value.ToString() : "no player")}";
        }
        #endregion
        
        #region Layout
        private VisualElement BuildPlayModeDebug()
        {
            var foldout = new Foldout { text = "Play Mode Debug", value = true, style = { marginTop = 8 } };

            playModeHelpBox = new HelpBox("Enter Play Mode and select the live Debugger (not the prefab asset) to enable below functionality.", HelpBoxMessageType.Info);
            foldout.Add(playModeHelpBox);

            playerStateLabel = new Label
            {
                style =
                {
                    unityFontStyleAndWeight = UnityEngine.FontStyle.Bold,
                    marginBottom = 4
                }
            };
            foldout.Add(playerStateLabel);

            foldout.Add(BuildSection("Combat",
                new PropertyField(serializedObject.FindProperty(FrankieDebugger.debugEnemiesField), "Enemies"),
                new PropertyField(serializedObject.FindProperty(FrankieDebugger.debugCombatTransitionTypeField), "Transition Type"),
                BuildActionButton("Enter Combat", debugger => debugger.DebugEnterCombat())));

            foldout.Add(BuildSection("Zone",
                new PropertyField(serializedObject.FindProperty(FrankieDebugger.debugZoneField), "Zone"),
                BuildZoneNodeField(),
                BuildActionButton("Enter Zone", debugger => debugger.DebugEnterZone())));

            foldout.Add(BuildSection("Player State",
                BuildActionButton("Enter Cutscene", debugger => debugger.DebugEnterCutscene()),
                BuildActionButton("Enter World", debugger => debugger.DebugEnterWorld())));

            foldout.Add(BuildSection("Party",
                new PropertyField(serializedObject.FindProperty(FrankieDebugger.debugStatusEffectStatField), "Status Effect Stat"),
                new PropertyField(serializedObject.FindProperty(FrankieDebugger.debugStatusEffectAmountField), "Amount"),
                new PropertyField(serializedObject.FindProperty(FrankieDebugger.debugStatusEffectDurationField), "Duration (s)"),
                BuildActionButton("Apply Status Effect", debugger => debugger.DebugApplyStatusEffect()),
                BuildActionButton("Knock Out Party", debugger => debugger.DebugKnockOutParty())));

            return foldout;
        }

        private static VisualElement BuildSection(string title, params VisualElement[] children)
        {
            var section = new VisualElement { style = { marginTop = 6 } };
            var header = new Label(title) { style = { unityFontStyleAndWeight = UnityEngine.FontStyle.Bold } };
            section.Add(header);

            foreach (VisualElement child in children) { section.Add(child); }
            return section;
        }

        private Button BuildActionButton(string text, Action<FrankieDebugger> action)
        {
            var button = new Button(() =>
            {
                if (IsLiveDebugger(out FrankieDebugger debugger)) { action?.Invoke(debugger); }
            }) { text = text };
            playModeOnlyElements.Add(button);
            return button;
        }

        private VisualElement BuildZoneNodeField()
        {
            SerializedProperty zoneProperty = serializedObject.FindProperty(FrankieDebugger.debugZoneField);
            SerializedProperty zoneNodeIDProperty = serializedObject.FindProperty(FrankieDebugger.debugZoneNodeIDField);

            zoneNodeField = new PopupField<string>("Node", new List<string> { _noZoneNode }, 0);
            zoneNodeField.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            zoneNodeField.RegisterValueChangedCallback(changeEvent =>
            {
                zoneNodeIDProperty.stringValue = changeEvent.newValue == _noZoneNode ? string.Empty : changeEvent.newValue;
                serializedObject.ApplyModifiedProperties();
            });
            zoneNodeField.TrackPropertyValue(zoneProperty, _ => RefreshZoneNodeChoices());
            RefreshZoneNodeChoices();
            return zoneNodeField;
        }

        private void RefreshZoneNodeChoices()
        {
            serializedObject.Update();
            var zone = serializedObject.FindProperty(FrankieDebugger.debugZoneField).objectReferenceValue as Zone;
            SerializedProperty zoneNodeIDProperty = serializedObject.FindProperty(FrankieDebugger.debugZoneNodeIDField);

            List<string> nodeIDs = zone != null ? zone.GetAllNodes().Where(zoneNode => zoneNode != null).Select(zoneNode => zoneNode.GetNodeID()).ToList() : new List<string>();
            if (nodeIDs.Count == 0) { nodeIDs.Add(_noZoneNode); }

            string selectedNodeID = nodeIDs.Contains(zoneNodeIDProperty.stringValue) ? zoneNodeIDProperty.stringValue : nodeIDs[0];
            zoneNodeField.choices = nodeIDs;
            zoneNodeField.value = selectedNodeID; // Fires the value callback ~ keeps the stored node ID valid for the zone
        }
        #endregion
    }
}
