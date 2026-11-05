using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;
using Unity.GraphToolkit.Editor;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Core;
using Frankie.Utils.Localization;

namespace Frankie.Speech.Editor
{
    internal static partial class DialogueGraphLocalizationSync
    {
        // Behavioural Notes:
        //  - see DialogueLocalizationPlan for reconciliation logic
        //  - tables are written immediately inside the edit's undo group
        //  - graph writes are deferred (since Graph Toolkit locks the graph during OnGraphChanged)

        // Const Tunables
        private const string _defaultLocale = "en";
        private const string _speakerField = "Speaker";
        private const string _textField = "Text";
        private const string _rekeyTitle = "Re-key Dialogue Lines";

        // State
        [AutoStaticsCleanup] public static bool suppressed; // Needed for tests (graphs opened in tests must not write the real tables)

        #region PublicMethods
        public static void HandleGraphChanged(DialogueGraph graph, bool fromUndoRedo, GraphLogger graphLogger)
        {
            if (!TryGetContext(graph, out string graphPath, out StringTableCollection collection)) { return; }
            Dictionary<string, string> bases = graph.GetLocalizationBases();

            List<DialogueLinePosition> positions = PlanPositions(graph, bases);
            DialogueLocalizationPlan plan = BuildPlan(graph, graphPath, collection, bases, positions, out Dictionary<string, string> baseIDsByKey);
            SettleBases(plan, bases);
            if (fromUndoRedo) { return; } // Undo / redo already restored both sides: only the bases need re-learning

            if (plan.hasTableWrites)
            {
                if (HasNameClash(graphPath)) { WarnNameClash(graph, graphPath, graphLogger); return; }
                ApplyTableWrites(plan, bases, baseIDsByKey);
            }
            ScheduleGraphWrites(graph, plan, hasPositions: positions.Count > 0);
        }

        public static void RekeyLines(DialogueGraph graph)
        {
            if (!TryGetContext(graph, out string graphPath, out _)) { return; }
            string dialogueName = GetDialogueName(graphPath);
            List<DialogueLinePosition> positions = DialogueLinePositionPlanner.PlanRekey(graph);
            if (!ConfirmRekey(dialogueName, positions.Count)) { return; }

            Undo.IncrementCurrentGroup();
            int editGroup = Undo.GetCurrentGroup();
            LocalizationTool.RenameKeys(LocalizationTableType.Speech, GetRekeyRenames(dialogueName, positions));
            SetPositions(graph, _rekeyTitle, editGroup, positions);
            LDMLog.Info(LDMLogCategory.Speech, $"Re-keyed {positions.Count} line(s) in dialogue '{dialogueName}'.");
        }
        #endregion

        #region Getters
        private static bool TryGetContext(DialogueGraph graph, out string graphPath, out StringTableCollection collection)
        {
            graphPath = graph != null ? GraphDatabase.GetGraphAssetPath(graph) : null;
            collection = null;
            if (suppressed || DialogueGraphImporter.isImporting || string.IsNullOrEmpty(graphPath) || !File.Exists(graphPath)) { return false; }
            return LocalizationTool.GetOrMakeTableCollection(LocalizationTableType.Speech, out collection) && collection.GetTable(_defaultLocale) is StringTable;
        }

        private static string GetDialogueName(string graphPath) => Path.GetFileNameWithoutExtension(graphPath);
        private static string GetKey(string dialogueName, int depth, int breadth, string field) => $"{dialogueName}.{depth}.{breadth}.{field}";
        private static string GetBaseID(string lineID, string field) => $"{lineID}.{field}";

        private static (string lineID, string field) SplitBaseID(string baseID)
        {
            int split = baseID.LastIndexOf('.');
            return (baseID[..split], baseID[(split + 1)..]);
        }

        private static HashSet<string> GetSyncedLineIDs(DialogueGraph graph, Dictionary<string, string> bases)
        {
            return new HashSet<string>(GetLines(graph).Select(DialogueLinePositionPlanner.GetID).Where(lineID => bases.ContainsKey(GetBaseID(lineID, _textField))));
        }

        private static IEnumerable<SpeechGraphNode> GetLines(DialogueGraph graph) => graph.GetNodes().OfType<SpeechGraphNode>();

        private static List<(string oldKey, string newKey)> GetRekeyRenames(string dialogueName, List<DialogueLinePosition> positions)
        {
            var renames = new List<(string oldKey, string newKey)>();
            foreach (DialogueLinePosition position in positions.Where(position => position.line.IsPlaced())) // Note:  Unplaced lines have no entries yet so nothing to rename
            {
                foreach (string field in new[] { _speakerField, _textField })
                {
                    renames.Add((GetKey(dialogueName, position.line.GetDepth(), position.line.GetBreadth(), field), GetKey(dialogueName, position.depth, position.breadth, field)));
                }
            }
            return renames;
        }
        
        private static bool HasNameClash(string graphPath)
        {
            string dialogueName = GetDialogueName(graphPath);
            return AssetDatabase.FindAssets($"t:{nameof(Dialogue)} {dialogueName}").Select(AssetDatabase.GUIDToAssetPath).Any(path => path != graphPath && GetDialogueName(path) == dialogueName);
        }
        
        private static Dictionary<string, string> GetTableEntries(StringTableCollection collection, string dialogueName)
        {
            var defaultTable = (StringTable)collection.GetTable(_defaultLocale);
            string keyPrefix = dialogueName + ".";
            return collection.SharedData.Entries.Where(entry => entry.Key.StartsWith(keyPrefix)).ToDictionary(entry => entry.Key, entry => defaultTable.GetEntry(entry.Id)?.Value);
        }
        
        private static List<DialogueLocalizationField> GetSyncableFields(DialogueGraph graph, string dialogueName, List<DialogueLinePosition> positions)
        {
            var moving = new HashSet<SpeechGraphNode>(positions.Select(position => position.line));
            var fields = new List<DialogueLocalizationField>();
            foreach (SpeechGraphNode line in GetLines(graph).Where(line => line.IsPlaced() && !moving.Contains(line))) // Only placed lines have keys && lines about to move sit out until they have their own key
            {
                string lineID = DialogueLinePositionPlanner.GetID(line);
                fields.Add(new DialogueLocalizationField(GetBaseID(lineID, _speakerField), GetKey(dialogueName, line.GetDepth(), line.GetBreadth(), _speakerField), line.GetSpeakerName()));
                fields.Add(new DialogueLocalizationField(GetBaseID(lineID, _textField), GetKey(dialogueName, line.GetDepth(), line.GetBreadth(), _textField), line.GetText()));
            }
            return fields;
        }
        #endregion

        #region PrivateMethods
        private static List<DialogueLinePosition> PlanPositions(DialogueGraph graph, Dictionary<string, string> bases) => DialogueLinePositionPlanner.Plan(graph, GetSyncedLineIDs(graph, bases));

        private static DialogueLocalizationPlan BuildPlan(DialogueGraph graph, string graphPath, StringTableCollection collection, Dictionary<string, string> bases, List<DialogueLinePosition> positions, out Dictionary<string, string> baseIDsByKey)
        {
            string dialogueName = GetDialogueName(graphPath);
            List<DialogueLocalizationField> fields = GetSyncableFields(graph, dialogueName, positions);
            baseIDsByKey = fields.ToDictionary(field => field.key, field => field.baseID);
            return new DialogueLocalizationPlan(fields, GetTableEntries(collection, dialogueName), bases);
        }

        private static void SettleBases(DialogueLocalizationPlan plan, Dictionary<string, string> bases)
        {
            foreach ((string baseID, string value) in plan.settledBases) { bases[baseID] = value; }
        }

        private static void ApplyTableWrites(DialogueLocalizationPlan plan, Dictionary<string, string> bases, Dictionary<string, string> baseIDsByKey)
        {
            foreach ((string key, string value) in plan.pushes)
            {
                LocalizationTool.AddUpdateEnglishEntry(LocalizationTableType.Speech, key, value);
                bases[baseIDsByKey[key]] = value;
            }
            foreach (string key in plan.removals) { LocalizationTool.RemoveEntry(LocalizationTableType.Speech, key); }
            foreach (string key in plan.conflicts) { LDMLog.Warning(LDMLogCategory.Speech, $"'{key}' was edited in both the dialogue graph and the Speech table: kept the graph's text"); }
        }

        private static void ScheduleGraphWrites(DialogueGraph graph, DialogueLocalizationPlan plan, bool hasPositions)
        {
            int editGroup = Undo.GetCurrentGroup();
            if (plan.hasGraphWrites) { EditorApplication.delayCall += () => ApplyPulls(graph, plan.pulls); }
            if (hasPositions) { EditorApplication.delayCall += () => ApplyPositions(graph, editGroup); }
        }

        private static void ApplyPulls(DialogueGraph graph, Dictionary<string, string> pulls)
        {
            var linesByID = GetLines(graph).ToDictionary(DialogueLinePositionPlanner.GetID);
            Dictionary<string, string> bases = graph.GetLocalizationBases();
            graph.RecordGraphChange("Sync Dialogue From Localization", Undo.GetCurrentGroup(), collapse: false, () =>
            {
                foreach ((string baseID, string value) in pulls)
                {
                    (string lineID, string field) = SplitBaseID(baseID);
                    if (!linesByID.TryGetValue(lineID, out SpeechGraphNode line)) { continue; }
                    line.GetNodeOptionByName(field == _speakerField ? SpeechGraphNode.speakerNameOption : SpeechGraphNode.textOption).TrySetValue(value);
                    bases[baseID] = value;
                }
            });
        }

        private static void ApplyPositions(DialogueGraph graph, int editGroup)
        {
            // Re-planned here: the graph may have changed since it was scheduled
            if (!TryGetContext(graph, out string graphPath, out StringTableCollection collection)) { return; }
            Dictionary<string, string> bases = graph.GetLocalizationBases();
            List<DialogueLinePosition> positions = PlanPositions(graph, bases);
            if (positions.Count == 0) { return; }

            SetPositions(graph, "Assign Dialogue Line Positions", editGroup, positions);
            PushNewlyPlacedLines(graph, graphPath, collection, bases, editGroup);
        }

        private static void SetPositions(DialogueGraph graph, string actionName, int editGroup, List<DialogueLinePosition> positions)
        {
            graph.RecordGraphChange(actionName, editGroup, collapse: true, () =>
            {
                foreach (DialogueLinePosition position in positions) { position.line.SetPosition(position.depth, position.breadth); }
            });
        }

        private static void PushNewlyPlacedLines(DialogueGraph graph, string graphPath, StringTableCollection collection, Dictionary<string, string> bases, int editGroup)
        {
            // Entries for the newly placed lines join the same undo step as the edit that placed them
            DialogueLocalizationPlan plan = BuildPlan(graph, graphPath, collection, bases, new List<DialogueLinePosition>(), out Dictionary<string, string> baseIDsByKey);
            if (!plan.hasTableWrites || HasNameClash(graphPath)) { return; }
            ApplyTableWrites(plan, bases, baseIDsByKey);
            Undo.CollapseUndoOperations(editGroup);
        }
        
        private static void WarnNameClash(DialogueGraph graph, string graphPath, GraphLogger graphLogger)
        {
            graphLogger.LogWarning($"Another dialogue is also named '{GetDialogueName(graphPath)}' -> localization keys will collide: rename one (lines aren't synced to the Speech table until then)", graph);
        }

        private static bool ConfirmRekey(string dialogueName, int lineCount)
        {
            if (lineCount == 0)
            {
                EditorUtility.DisplayDialog(_rekeyTitle, $"'{dialogueName}' is already numbered in order.", "OK");
                return false;
            }
            return EditorUtility.DisplayDialog(_rekeyTitle, $"Renumber {lineCount} line(s) in '{dialogueName}'? Their localization keys change (translations move with them), so translators' references to the old keys break.", "Re-key", "Cancel");
        }
        #endregion
    }
}
