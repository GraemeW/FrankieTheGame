using System.Collections.Generic;
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

        [Header("Editor Settings")]
        [SerializeField] Vector2 newNodeOffset = new Vector2(100f, 25f);
        [SerializeField] int nodeWidth = 400;
        [SerializeField] int nodeHeight = 225;

        // State
        [HideInInspector] [SerializeField] List<CharacterProperties> activeNPCs = null;
        [HideInInspector] [SerializeField] List<DialogueNode> dialogueNodes = new List<DialogueNode>();
        [HideInInspector] [SerializeField] Dictionary<string, DialogueNode> nodeLookup = new Dictionary<string, DialogueNode>();

#if UNITY_EDITOR
        private void Awake()
        {
            CreateRootNodeIfMissing();
        }
#endif

        private void OnValidate()
        {
            nodeLookup = new Dictionary<string, DialogueNode>();
            activeNPCs = new List<CharacterProperties>();
            foreach (DialogueNode dialogueNode in dialogueNodes)
            {
                nodeLookup.Add(dialogueNode.name, dialogueNode);
                if (dialogueNode.GetCharacterProperties() != null && !activeNPCs.Contains(dialogueNode.GetCharacterProperties()))
                {
                    activeNPCs.Add(dialogueNode.GetCharacterProperties());
                }
            }
        }

        public IEnumerable<DialogueNode> GetAllNodes()
        {
            return dialogueNodes;
        }

        public DialogueNode GetRootNode(bool withSkip = true)
        {
            return dialogueNodes[0];
        }

        public DialogueNode GetNodeFromID(string name)
        {
            foreach (DialogueNode dialogueNode in dialogueNodes)
            {
                if (dialogueNode.name == name)
                {
                    return dialogueNode;
                }
            }
            return null;
        }

        public IEnumerable<DialogueNode> GetAllChildren(DialogueNode parentNode)
        {
            if (parentNode == null || parentNode.GetChildren() == null || parentNode.GetChildren().Count == 0) { yield break; }
            foreach (string childUniqueID in parentNode.GetChildren())
            {
                if (nodeLookup.ContainsKey(childUniqueID))
                {
                    yield return nodeLookup[childUniqueID];
                }
            }
        }

        public bool IsRelated(DialogueNode parentNode, DialogueNode childNode)
        {
            if (parentNode.GetChildren().Contains(childNode.name))
            {
                return true;
            }
            return false;
        }

        public void OverrideSpeakerNames(string playerName)
        {
            foreach (DialogueNode dialogueNode in dialogueNodes)
            {
                SpeakerType speakerType = dialogueNode.GetSpeakerType();
                if (speakerType == SpeakerType.playerSpeaker && IsPlayerNameOverrideable(playerName)) { dialogueNode.SetSpeakerName(playerName); }
                else if (speakerType == SpeakerType.aiSpeaker && IsSpeakerNameOverrideable(speakerType, dialogueNode))
                { 
                    dialogueNode.SetSpeakerName(CharacterProperties.GetStaticCharacterNamePretty(dialogueNode.GetCharacterName())); 
                }
            }
        }

        private bool IsPlayerNameOverrideable(string playerName)
        {
            return !string.IsNullOrWhiteSpace(playerName);
        }

        private bool IsSpeakerNameOverrideable(SpeakerType speakerType, DialogueNode dialogueNode)
        {

            if (speakerType == SpeakerType.aiSpeaker) 
            { 
                return (dialogueNode.GetCharacterName() != default 
                    && !string.IsNullOrWhiteSpace(CharacterProperties.GetStaticCharacterNamePretty(dialogueNode.GetCharacterName()))); 
            }
            return false;
        }

        public List<CharacterProperties> GetActiveCharacters()
        {
            return activeNPCs;
        }

        // Dialogue editing functionality
#if UNITY_EDITOR
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
            Vector2 offsetPosition = new Vector2(parentNode.GetRect().xMax + newNodeOffset.x,
                parentNode.GetRect().yMin + (parentNode.GetRect().height + newNodeOffset.y) * parentNode.GetChildren().Count);
            childNode.SetPosition(offsetPosition);
            if (parentNode.GetSpeakerType() == SpeakerType.playerSpeaker)
            {
                childNode.SetSpeakerType(SpeakerType.aiSpeaker);
            }
            else
            {
                childNode.SetSpeakerType(SpeakerType.playerSpeaker);
            }

            parentNode.AddChild(childNode.name);
            OnValidate();

            return childNode;
        }

        private DialogueNode CreateRootNodeIfMissing()
        {
            if (dialogueNodes.Count == 0)
            {
                DialogueNode rootNode = CreateNode();

                OnValidate();
                return rootNode;
            }

            return null;
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
            foreach (DialogueNode dialogueNode in dialogueNodes)
            {
                if (dialogueNode.GetCharacterName() == speaker)
                {
                    dialogueNode.SetSpeakerName(newSpeakerName);
                }
            }
        }
#endif

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (AssetDatabase.GetAssetPath(this) != "")
            {
                foreach (DialogueNode dialogueNode in GetAllNodes())
                {
                    if (AssetDatabase.GetAssetPath(dialogueNode) == "")
                    {
                        AssetDatabase.AddObjectToAsset(dialogueNode, this);
                    }
                }
            }
#endif
        }

        public void OnAfterDeserialize() // Unused, required for interface
        {
        }
    }
}