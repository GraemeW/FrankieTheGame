using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Speech
{
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/New Dialogue")]
    public class Dialogue : ScriptableObject, ISerializationCallbackReceiver
    {
        // Tunables
        [SerializeField] public bool skipRootNode = false;

#if UNITY_EDITOR
        [Header("Editor Settings")]
        [SerializeField] private Vector2 newNodeOffset = new Vector2(100f, 25f);
        [SerializeField] private int nodeWidth = 400;
        [SerializeField] private int nodeHeight = 225;
#endif

        // State
        [HideInInspector][SerializeField] private List<CharacterProperties> activeNPCs;
        [HideInInspector][SerializeField] private List<DialogueNode> dialogueNodes = new();
#if UNITY_EDITOR
        private Dictionary<string, DialogueNode> nodeEditorLookup = new();
#endif

        #region StaticMethods
        public static bool IsRelated(DialogueNode parentNode, DialogueNode childNode) => parentNode.GetChildren() != null && parentNode.GetChildren().Contains(childNode.name);
        private static bool IsPlayerNameOverrideable(string playerName) => !string.IsNullOrWhiteSpace(playerName);
        private static bool IsSpeakerNameOverrideable(SpeakerType speakerType, DialogueNode dialogueNode)
        {
            if (speakerType != SpeakerType.AISpeaker) { return false; }
            return dialogueNode.GetCharacterName() != null
                   && !string.IsNullOrWhiteSpace(CharacterProperties.GetStaticCharacterNamePretty(dialogueNode.GetCharacterName()));
        }
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
                nodeEditorLookup.Add(dialogueNode.name, dialogueNode);
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

        public DialogueNode GetRootNode(bool withSkip = true) => dialogueNodes[0];
        public DialogueNode GetNodeFromID(string nodeID) => dialogueNodes.FirstOrDefault(dialogueNode => dialogueNode.name == nodeID);
        public List<CharacterProperties> GetActiveCharacters() => activeNPCs;

        public void OverrideSpeakerNames(string playerName)
        {
            foreach (DialogueNode dialogueNode in dialogueNodes)
            {
                SpeakerType speakerType = dialogueNode.GetSpeakerType();
                if (speakerType == SpeakerType.PlayerSpeaker && IsPlayerNameOverrideable(playerName)) { dialogueNode.SetSpeakerName(playerName); }
                else if (speakerType == SpeakerType.AISpeaker && IsSpeakerNameOverrideable(speakerType, dialogueNode))
                {
                    dialogueNode.SetSpeakerName(CharacterProperties.GetStaticCharacterNamePretty(dialogueNode.GetCharacterName()));
                }
            }
        }
        #endregion
        

        #region EditorMethods
#if UNITY_EDITOR
        public IEnumerable<DialogueNode> GetAllChildren(DialogueNode parentNode)
        {
            if (parentNode == null || parentNode.GetChildren() == null || parentNode.GetChildren().Count == 0) { yield break; }
            foreach (var childUniqueID in parentNode.GetChildren().Where(childUniqueID => nodeEditorLookup.ContainsKey(childUniqueID)))
            {
                yield return nodeEditorLookup[childUniqueID];
            }
        }
        
        private DialogueNode CreateNode()
        {
            DialogueNode dialogueNode = CreateInstance<DialogueNode>();
            Undo.RegisterCreatedObjectUndo(dialogueNode, "Created Dialogue Node Object");
            dialogueNode.Initialize(nodeWidth, nodeHeight);
            dialogueNode.name = System.Guid.NewGuid().ToString();
            dialogueNode.SetText("Default Text to Overwrite");

            Undo.RecordObject(this, "Add Dialogue Node");
            dialogueNodes.Add(dialogueNode);

            return dialogueNode;
        }

        public DialogueNode CreateChildNode(DialogueNode parentNode)
        {
            if (parentNode == null) { return null; }

            DialogueNode childNode = CreateNode();
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
            if (dialogueNodes.Count != 0) return;
            CreateNode();
            OnValidate();
        }

        public void DeleteNode(DialogueNode nodeToDelete)
        {
            if (nodeToDelete == null) { return; }

            Undo.RecordObject(this, "Delete Dialogue Node");
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
            foreach (DialogueNode dialogueNode in dialogueNodes.Where(dialogueNode => dialogueNode.GetCharacterName() == speaker))
            {
                dialogueNode.SetSpeakerName(newSpeakerName);
            }
        }

        public void RegenerateGUIDs()
        {
            Undo.RegisterCompleteObjectUndo(this, "Regenerate GUIDs");
            foreach (DialogueNode dialogueNode in dialogueNodes)
            {
                dialogueNode.name = System.Guid.NewGuid().ToString();
            }
            EditorUtility.SetDirty(this);
        }
#endif
        #endregion

        #region InterfaceMethods
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (AssetDatabase.GetAssetPath(this) == "") return;
            foreach (DialogueNode dialogueNode in GetAllNodes())
            {
                if (AssetDatabase.GetAssetPath(dialogueNode) == "")
                {
                    AssetDatabase.AddObjectToAsset(dialogueNode, this);
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
