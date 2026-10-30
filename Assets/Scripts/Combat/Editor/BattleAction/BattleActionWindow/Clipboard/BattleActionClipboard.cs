using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Combat.Editor
{
    public static class BattleActionClipboard
    {
        // Const Tunables
        private const string _scriptField = "m_Script";

        // State
        [NoAutoStaticsCleanup] private static BattleAction _copy;
        [NoAutoStaticsCleanup] private static BattleActionCopyKind _kind;

        #region PublicMethods
        public static void AppendActions(DropdownMenu menu, BattleAction battleAction, BattleActionCopyKind kind, System.Action pasted)
        {
            menu.AppendAction($"Copy{GetSuffix(kind)}", _ => Copy(battleAction, kind));
            if (kind == BattleActionCopyKind.All)
            {
                menu.AppendAction($"Copy{GetSuffix(BattleActionCopyKind.Targeting)}", _ => Copy(battleAction, BattleActionCopyKind.Targeting));
                menu.AppendAction($"Copy{GetSuffix(BattleActionCopyKind.EffectStrategies)}", _ => Copy(battleAction, BattleActionCopyKind.EffectStrategies));
            }
            menu.AppendSeparator();

            BattleActionCopyKind pasteKind = kind == BattleActionCopyKind.All && _copy != null ? _kind : kind;
            string pasteLabel = _copy == null ? "Paste" : $"Paste{GetSuffix(pasteKind)}  (from {_copy.name})";
            menu.AppendAction(pasteLabel, _ =>
            {
                if (Paste(battleAction, pasteKind)) { pasted?.Invoke(); }
            }, _ => CanPaste(pasteKind) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }
        #endregion

        #region Actions
        private static void Copy(BattleAction source, BattleActionCopyKind kind)
        {
            if (source == null) { return; }
            if (_copy != null) { Object.DestroyImmediate(_copy); }
            _copy = Object.Instantiate(source);
            _copy.name = source.name;
            _copy.hideFlags = HideFlags.HideAndDontSave;
            _kind = kind;
        }

        private static bool CanPaste(BattleActionCopyKind kind) => _copy != null && (_kind == BattleActionCopyKind.All || _kind == kind);

        private static bool Paste(BattleAction target, BattleActionCopyKind kind)
        {
            if (target == null || !CanPaste(kind)) { return false; }

            var source = new SerializedObject(_copy);
            var destination = new SerializedObject(target);
            foreach (string path in GetPaths(source, kind)) { destination.CopyFromSerializedProperty(source.FindProperty(path)); }
            destination.ApplyModifiedProperties();
            Undo.SetCurrentGroupName($"Paste Battle Action{GetSuffix(kind)}");
            return true;
        }
        #endregion
        
        #region Utility
        private static string GetSuffix(BattleActionCopyKind kind) => kind switch
        {
            BattleActionCopyKind.Targeting => " Targeting",
            BattleActionCopyKind.EffectStrategies => " Effect Strategies",
            _ => string.Empty
        };

        private static IEnumerable<string> GetPaths(SerializedObject source, BattleActionCopyKind kind)
        {
            switch (kind)
            {
                case BattleActionCopyKind.Targeting:
                    yield return BattleAction.targetingStrategyField;
                    break;
                case BattleActionCopyKind.EffectStrategies:
                    yield return BattleAction.effectStrategiesField;
                    break;
                case BattleActionCopyKind.All:
                default:
                    SerializedProperty property = source.GetIterator();
                    for (bool hasProperty = property.NextVisible(true); hasProperty; hasProperty = property.NextVisible(false))
                    {
                        if (property.name != _scriptField) { yield return property.propertyPath; }
                    }
                    break;
            }
        }
        #endregion
    }
}
