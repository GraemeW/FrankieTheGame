using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Tables;
#if UNITY_EDITOR
using UnityEditor;
#endif
using LowDefMustard.Core;
using LowDefMustard.Utils;
using Frankie.Stats;
using Frankie.Utils.Localization;

namespace Frankie.Speech
{
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/New Dialogue", order = 10)]
    public class Dialogue : ScriptableObject, ISerializationCallbackReceiver, ILocalizable
    {
        // Tunables
        [SerializeField] public bool skipRootNode = false; // Legacy (pre-graph) dialogues only - To-Be-Removed (pending)

     
        // Const
        private const string _defaultSpeakerName = "DefaultSpeaker";
        private const string _defaultText = "Default Text to Overwrite";
        
#if UNITY_EDITOR
        [Header("Editor Settings")]
        [HideInInspector][SerializeField] private string cachedName = "";
        public string iCachedName { get => cachedName; set => cachedName = value; }
        [SerializeField] private Vector2 newNodeOffset = new(100f, 25f);
        [SerializeField] private int nodeWidth = 400;
        [SerializeField] private int nodeHeight = 225;
#endif

        // State
        [HideInInspector][SerializeField] private List<CharacterProperties> activeNPCs;
        [HideInInspector][SerializeField] private List<DialogueNode> dialogueNodes = new();
        [HideInInspector][SerializeField] private List<DialogueConditionNode> conditionNodes = new();
        [HideInInspector][SerializeField] private List<string> entryNodeIDs = new();
#if UNITY_EDITOR
        private Dictionary<string, DialogueNode> nodeEditorLookup = new();
#endif

        #region StaticMethods
        public static bool IsRelated(DialogueNode parentNode, DialogueNode childNode) => parentNode.GetChildren() != null && parentNode.GetChildren().Contains(childNode.GetNodeID());
        private static bool IsPlayerNameOverrideable(string playerName) => !string.IsNullOrWhiteSpace(playerName);
        private static bool IsSpeakerNameOverrideable(SpeakerType speakerType, DialogueNode dialogueNode) => speakerType == SpeakerType.AISpeaker && dialogueNode.HasValidCharacterProperties();
        #endregion
        
        #region UnityMethods
        private void Awake()
        {
#if UNITY_EDITOR
            //CreateRootNodeIfMissing();
            // Note:  Making in DialogueEditor due to some intricacies in how Unity calls Awake on ScriptableObjects in Editor vs. the serialization callback
            // For (unknown) reasons, the root node gets made and then killed by the time serialization occurs
#endif
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            nodeEditorLookup = new Dictionary<string, DialogueNode>();
            activeNPCs = new List<CharacterProperties>();

            if (dialogueNodes == null) { return; }
            foreach (DialogueNode dialogueNode in dialogueNodes.TakeWhile(dialogueNode => dialogueNode != null))
            {
                nodeEditorLookup.Add(dialogueNode.GetNodeID(), dialogueNode);
                if (dialogueNode.GetCharacterProperties() != null && !activeNPCs.Contains(dialogueNode.GetCharacterProperties()))
                {
                    activeNPCs.Add(dialogueNode.GetCharacterProperties());
                }
            }
#endif
        }
        #endregion

        #region GettersSetters
        public IEnumerable<DialogueNode> GetAllNodes() => dialogueNodes;
        public DialogueNode GetRootNode(bool withSkip = true) => dialogueNodes is { Count: > 0 } ? dialogueNodes[0] : null;
        public DialogueNode GetNodeFromID(string nodeID) => dialogueNodes.FirstOrDefault(dialogueNode => dialogueNode != null && dialogueNode.GetNodeID() == nodeID);
        private DialogueConditionNode GetConditionNodeFromID(string nodeID) => conditionNodes.FirstOrDefault(conditionNode => conditionNode != null && conditionNode.GetNodeID() == nodeID);
        public List<CharacterProperties> GetActiveCharacters() => activeNPCs;

        public void OverrideSpeakerNames(string playerName)
        {
            foreach (DialogueNode dialogueNode in dialogueNodes)
            {
                SpeakerType speakerType = dialogueNode.GetSpeakerType();
                if (speakerType == SpeakerType.PlayerSpeaker && IsPlayerNameOverrideable(playerName)) { dialogueNode.OverrideSpeakerName(playerName); }
                else if (speakerType == SpeakerType.AISpeaker && IsSpeakerNameOverrideable(speakerType, dialogueNode)) { dialogueNode.OverrideSpeakerName(); }
            }
        }
        
        #endregion

        #region Traversal
        public List<DialogueNode> ResolveEntry(IEnumerable<IPredicateEvaluator> evaluators)
        {
            if (entryNodeIDs.Count > 0) { return ResolveTargets(entryNodeIDs, evaluators); }

            // Legacy (pre-graph) dialogues start at the root node, or at its children when the root is skipped - To-Be-Removed (pending)
            DialogueNode rootNode = GetRootNode();
            if (rootNode == null) { return new List<DialogueNode>(); }
            return skipRootNode ? ResolveTargets(rootNode.GetChildren(), evaluators) : new List<DialogueNode> { rootNode };
        }

        public List<DialogueNode> ResolveChildren(DialogueNode dialogueNode, IEnumerable<IPredicateEvaluator> evaluators)
        {
            return dialogueNode != null ? ResolveTargets(dialogueNode.GetChildren(), evaluators) : new List<DialogueNode>();
        }

        private List<DialogueNode> ResolveTargets(IEnumerable<string> nodeIDs, IEnumerable<IPredicateEvaluator> evaluators)
        {
            var resolvedNodes = new List<DialogueNode>();
            AppendResolvedTargets(nodeIDs, evaluators.ToList(), resolvedNodes, new HashSet<DialogueConditionNode>());
            return resolvedNodes;
        }

        private void AppendResolvedTargets(IEnumerable<string> nodeIDs, List<IPredicateEvaluator> evaluators, List<DialogueNode> resolvedNodes, HashSet<DialogueConditionNode> visitedConditionNodes)
        {
            if (nodeIDs == null) { return; }
            foreach (string nodeID in nodeIDs)
            {
                DialogueNode dialogueNode = GetNodeFromID(nodeID);
                if (dialogueNode != null)
                {
                    // Per-node condition only exists on legacy (pre-graph) dialogues - To-Be-Removed (pending)
                    if (!resolvedNodes.Contains(dialogueNode) && dialogueNode.CheckCondition(evaluators)) { resolvedNodes.Add(dialogueNode); }
                    continue;
                }

                DialogueConditionNode conditionNode = GetConditionNodeFromID(nodeID);
                if (conditionNode == null || !visitedConditionNodes.Add(conditionNode)) { continue; }
                if (!conditionNode.CheckCondition(evaluators)) { continue; }
                AppendResolvedTargets(conditionNode.GetChildren(), evaluators, resolvedNodes, visitedConditionNodes);
            }
        }
        #endregion

        #region Localization
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.Speech;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            var entries = new List<TableEntryReference>();
            foreach (DialogueNode dialogueNode in dialogueNodes.Where(dialogueNode => dialogueNode != null))
            {
                entries.AddRange(dialogueNode.GetLocalizationEntries());
            }
            return entries;
        }
        #endregion

#if UNITY_EDITOR
        #region EditorMethods
        internal void InitializeGraph(IEnumerable<DialogueNode> setDialogueNodes, IEnumerable<DialogueConditionNode> setConditionNodes, IEnumerable<string> setEntryNodeIDs)
        {
            dialogueNodes = new List<DialogueNode>(setDialogueNodes);
            conditionNodes = new List<DialogueConditionNode>(setConditionNodes);
            entryNodeIDs = new List<string>(setEntryNodeIDs);
            OnValidate();
        }

        public IEnumerable<DialogueNode> GetAllChildren(DialogueNode parentNode)
        {
            if (parentNode == null || parentNode.GetChildren() == null || parentNode.GetChildren().Count == 0) { yield break; }
            foreach (var childUniqueID in parentNode.GetChildren().Where(childUniqueID => nodeEditorLookup.ContainsKey(childUniqueID)))
            {
                yield return nodeEditorLookup[childUniqueID];
            }
        }
        
        private DialogueNode CreateNode(int depth, int breadth)
        {
            var dialogueNode = CreateInstance<DialogueNode>();
            Undo.RegisterCreatedObjectUndo(dialogueNode, "Created Dialogue Node Object");
            dialogueNode.Initialize(nodeWidth, nodeHeight);
            dialogueNode.name = System.Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);
            dialogueNode.SetNodeDepthBreadth(depth, breadth, forceRenameNode: false);
            dialogueNode.SetDialogueName(name, tryRename: false);
            dialogueNode.SetSpeakerName(_defaultSpeakerName);
            dialogueNode.SetText(_defaultText);

            Undo.RecordObject(this, "Add Dialogue Node");
            dialogueNodes.Add(dialogueNode);

            return dialogueNode;
        }

        public DialogueNode CreateChildNode(DialogueNode parentNode)
        {
            if (parentNode == null) { return null; }

            int childDepth = parentNode.GetNodeDepth() + 1;
            int checkBreadth = -1;
            foreach (DialogueNode checkNode in dialogueNodes.Where(checkNode => checkNode.GetNodeDepth() == childDepth))
            {
                checkBreadth = Mathf.Max(checkBreadth, checkNode.GetNodeBreadth());
            }
            int childBreadth = checkBreadth + 1;
            LDMLog.Info(LDMLogCategory.Speech, $"Adding dialogue node to '{name}' (depth: {childDepth}, breadth: {childBreadth}).", this);
            
            DialogueNode childNode = CreateNode(childDepth, childBreadth);
            childNode.SetSpeakerType(parentNode.GetSpeakerType() == SpeakerType.PlayerSpeaker ? SpeakerType.AISpeaker : SpeakerType.PlayerSpeaker);
            parentNode.AddChild(childNode.name);

            var offsetPosition = new Vector2(parentNode.GetRect().xMax + newNodeOffset.x,
                parentNode.GetRect().yMin + (parentNode.GetRect().height + newNodeOffset.y) * (parentNode.GetChildren().Count - 1));  // Offset position by 1 since child just added
            childNode.SetPosition(offsetPosition);

            OnValidate();

            return childNode;
        }

        public void CreateRootNodeIfMissing()
        {
            if (dialogueNodes.Count != 0) { return; }
            CreateNode(0,0);
            OnValidate();
        }

        public void DeleteNode(DialogueNode nodeToDelete)
        {
            if (nodeToDelete == null) { return; }

            Undo.RecordObject(this, "Delete Dialogue Node");
            nodeToDelete.DeleteLocalizationEntries();
            dialogueNodes.Remove(nodeToDelete);
            CleanDanglingChildren(nodeToDelete);
            OnValidate();

            Undo.DestroyObjectImmediate(nodeToDelete);
        }

        private void CleanDanglingChildren(DialogueNode nodeToDelete)
        {
            foreach (DialogueNode dialogueNode in dialogueNodes)
            {
                dialogueNode.RemoveChild(nodeToDelete.name);
            }
        }

        public void ToggleRelation(DialogueNode parentNode, DialogueNode childNode)
        {
            if (IsRelated(parentNode, childNode))
            {
                parentNode.RemoveChild(childNode.name);
            }
            else
            {
                parentNode.AddChild(childNode.name);
            }
            OnValidate();
        }

        public void UpdateSpeakerName(string speaker, string newSpeakerName)
        {
            foreach (DialogueNode dialogueNode in dialogueNodes.Where(dialogueNode => dialogueNode.GetSpeakerName(false) == speaker))
            {
                dialogueNode.SetSpeakerName(newSpeakerName);
            }
        }

        public void RegenerateGUIDs()
        {
            var guidSwapCache = new Dictionary<string, string>();
            foreach (DialogueNode dialogueNode in dialogueNodes)
            {
                guidSwapCache[dialogueNode.name] = System.Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);
            }
            
            Undo.RegisterCompleteObjectUndo(this, "Regenerate GUIDs");
            foreach (DialogueNode dialogueNode in dialogueNodes.Where(dialogueNode => guidSwapCache.ContainsKey(dialogueNode.name)))
            {
                dialogueNode.name = guidSwapCache[dialogueNode.name];
                List<string> childNodes = dialogueNode.GetChildren().ToList();
                foreach (string childNode in childNodes.Where(guidSwapCache.ContainsKey))
                {
                    dialogueNode.SwapChild(childNode, guidSwapCache[childNode], false);
                }
            }
            EditorUtility.SetDirty(this);
        }

        public void ReserializeNodeDepthBreadth()
        {
            var nodesToTraverse = new Queue<DialogueNode>();
            HashSet<string> visitedNodes = new HashSet<string>();
            
            DialogueNode rootNode = GetRootNode();
            if (rootNode == null) { return; }
            nodesToTraverse.Enqueue(rootNode);

            int currentDepth = 0;
            int currentBreadth = 0;
            while (nodesToTraverse.Count > 0)
            {
                DialogueNode currentNode = nodesToTraverse.Dequeue();
                visitedNodes.Add(currentNode.name);
                
                int checkDepth = currentNode.GetNodeDepth();
                if (checkDepth > currentDepth) { currentBreadth = 0; } 
                currentDepth = checkDepth;
                
                foreach (DialogueNode childNode in GetAllChildren(currentNode))
                {
                    if (childNode == null) { continue; }
                    
                    if (!visitedNodes.Contains(childNode.name))
                    {
                        childNode.SetNodeDepthBreadth(currentDepth + 1, currentBreadth, forceRenameNode: true);
                        nodesToTraverse.Enqueue(childNode);
                    }
                    currentBreadth++;
                }
            }
        }

        public void TriggerOnRename()
        {
            foreach (DialogueNode dialogueNode in dialogueNodes)
            {
                dialogueNode.SetDialogueName(name);
            }
        }
        #endregion
#endif

        #region SerializationInterface
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (AssetDatabase.GetAssetPath(this) == "") return;
            
            foreach (ScriptableObject node in dialogueNodes.Cast<ScriptableObject>().Concat(conditionNodes))
            {
                if (node == null) { continue; }
                if (AssetDatabase.GetAssetPath(node) == "")
                {
                    AssetDatabase.AddObjectToAsset(node, this);
                }
            }
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Unused, required for interface
        }
        #endregion
    }
}
