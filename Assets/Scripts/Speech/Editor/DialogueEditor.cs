using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using Frankie.Utils.Localization;
using Frankie.Utils.Editor;

namespace Frankie.Speech.Editor
{
    public class DialogueEditor : EditorWindow
    {
        // Const Tunables
        const string _defaultSpeakerName = "DefaultSpeaker";

        // Model
        private Dialogue selectedDialogue;
        private DialogueNode linkingParentNode;
        private readonly Dictionary<string, DialogueNodeView> nodeViews = new();

        // UI Elements
        private Label dialogueNameLabel;
        private Label noDialogueLabel;
        private VisualElement viewport;
        private VisualElement canvasContent;
        private StandardCanvasZoomManipulator zoomManipulator;
        private VisualElement backgroundLayer;
        private DialogueConnectionsLayer connectionsLayer;
        private VisualElement nodesLayer;

        #region UnityMethods
        [MenuItem("Window/Dialogue Editor")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(DialogueEditor), false, "Dialogue Editor");
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(EntityId instanceID, int line)
        {
            var dialogue = EditorUtility.EntityIdToObject(instanceID) as Dialogue;
            if (dialogue == null) return false;

            if (dialogue is ILocalizable localizable)
            {
                // Note:  No standard entries on dialogue itself -- needed for triggering node re-name on dialogue asset re-name
                var emptyStandardEntryList = new List<(string propertyName, LocalizedString localizedString, bool setToName)>();
                localizable.TryLocalizeStandardEntries(dialogue, emptyStandardEntryList, dialogue.TriggerOnRename);
            }

            dialogue.CreateRootNodeIfMissing();
            ShowEditorWindow();
            return true;
        }

        private void OnEnable()
        {
            LocalizationTool.InitializeEnglishLocale();
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }
        #endregion

        #region DrawingMethods
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            nodeViews.Clear();

            dialogueNameLabel = MakeDialogueNameLabel();
            rootVisualElement.Add(dialogueNameLabel);

            noDialogueLabel = new Label("No dialogue selected.") { style = { paddingLeft = 6 } };
            rootVisualElement.Add(noDialogueLabel);

            viewport = MakeViewport();
            viewport.RegisterCallback<MouseDownEvent>(_ => Selection.activeObject = selectedDialogue);
            rootVisualElement.Add(viewport);

            canvasContent = MakeCanvas();
            viewport.Add(canvasContent);
            viewport.AddManipulator(new StandardCanvasPanManipulator(canvasContent));
            zoomManipulator = new StandardCanvasZoomManipulator(canvasContent);
            zoomManipulator.zoomChanged += zoom => connectionsLayer.SetZoomFactor(zoom);
            viewport.AddManipulator(zoomManipulator);
            
            backgroundLayer = new StandardBackgroundLayer(StandardBackgroundType.Dots);
            canvasContent.Add(backgroundLayer);

            connectionsLayer = new DialogueConnectionsLayer();
            canvasContent.Add(connectionsLayer);

            nodesLayer = MakeNodesLayer();
            canvasContent.Add(nodesLayer);

            selectedDialogue = Selection.activeObject as Dialogue;
            RebuildCanvas();
        }

        private void RebuildCanvas()
        {
            nodesLayer?.Clear();
            nodeViews?.Clear();
            linkingParentNode = null;

            bool hasDialogue = selectedDialogue != null;
            if (noDialogueLabel != null) { noDialogueLabel.style.display = hasDialogue ? DisplayStyle.None : DisplayStyle.Flex; }
            if (viewport != null) { viewport.style.display = hasDialogue ? DisplayStyle.Flex : DisplayStyle.None; }
            if (dialogueNameLabel != null) { dialogueNameLabel.text = hasDialogue ? selectedDialogue.name : string.Empty; }
            
            if (!hasDialogue) { return; }

            connectionsLayer?.SetDialogue(selectedDialogue);
            foreach (DialogueNode dialogueNode in selectedDialogue.GetAllNodes())
            {
                if (dialogueNode == null) { continue; }
                AddNodeView(dialogueNode);
            }
            RefreshLinkButtons();
        }

        private void AddNodeView(DialogueNode dialogueNode)
        {
            if (nodeViews == null) { return; }
            var view = new DialogueNodeView(dialogueNode, selectedDialogue, MarkConnectionsDirty, () => zoomManipulator.zoomFactor);
            view.speakerNameChanged += HandleSpeakerNameChanged;
            view.speakerTypeChanged += HandleSpeakerTypeChanged;
            view.textChanged += HandleTextChanged;
            view.deleteRequested += HandleDeleteRequested;
            view.createChildRequested += HandleCreateChildRequested;
            view.linkClicked += HandleLinkClicked;

            nodesLayer.Add(view);
            nodeViews[dialogueNode.name] = view;
        }

        private void RemoveNodeView(DialogueNode dialogueNode)
        {
            if (dialogueNode == null) { return; }
            if (!nodeViews.TryGetValue(dialogueNode.name, out DialogueNodeView view)) { return; }
            nodesLayer.Remove(view);
            nodeViews.Remove(dialogueNode.name);
        }

        private void MarkConnectionsDirty() => connectionsLayer?.MarkDirtyRepaint();

        private void RefreshCharacterBackgrounds()
        {
            foreach (DialogueNodeView view in nodeViews.Values)
            {
                view.RefreshVisualStyle();
            }
        }
        #endregion

        #region EventHandlers
        private void OnSelectionChanged()
        {
            var newDialogue = Selection.activeObject as Dialogue;
            if (newDialogue == null || newDialogue == selectedDialogue) { return; }

            selectedDialogue = newDialogue;
            RebuildCanvas();
        }
        
        private void HandleSpeakerNameChanged(DialogueNode dialogueNode, string newSpeakerName)
        {
            bool speakerNameChanged = dialogueNode.SetSpeakerName(newSpeakerName);
            if (dialogueNode.GetSpeakerType() != SpeakerType.PlayerSpeaker && speakerNameChanged)
            {
                selectedDialogue.UpdateSpeakerName(dialogueNode.GetSpeakerName(false), newSpeakerName);
            }
            RefreshCharacterBackgrounds();
        }

        private void HandleSpeakerTypeChanged(DialogueNode dialogueNode, SpeakerType newSpeakerType)
        {
            if (newSpeakerType == dialogueNode.GetSpeakerType()) { return; }

            dialogueNode.SetSpeakerType(newSpeakerType);
            dialogueNode.SetSpeakerName(_defaultSpeakerName);

            if (nodeViews.TryGetValue(dialogueNode.name, out DialogueNodeView view))
            {
                view.SetSpeakerNameFieldValue(_defaultSpeakerName);
            }

            RefreshCharacterBackgrounds();
        }

        private void HandleTextChanged(DialogueNode dialogueNode, string newText) => dialogueNode.SetText(newText);

        private void HandleDeleteRequested(DialogueNode dialogueNode)
        {
            if (linkingParentNode == dialogueNode) { linkingParentNode = null; }

            RemoveNodeView(dialogueNode);
            selectedDialogue.DeleteNode(dialogueNode);
            RefreshCharacterBackgrounds();
            RefreshLinkButtons();
            MarkConnectionsDirty();
        }

        private void HandleCreateChildRequested(DialogueNode parentNode)
        {
            DialogueNode newNode = selectedDialogue.CreateChildNode(parentNode);
            if (newNode == null) { return; }

            AddNodeView(newNode);
            RefreshCharacterBackgrounds();
            RefreshLinkButtons();
            MarkConnectionsDirty();
        }

        private void HandleLinkClicked(DialogueNode dialogueNode)
        {
            if (linkingParentNode == null)
            {
                linkingParentNode = dialogueNode;
            }
            else if (dialogueNode == linkingParentNode)
            {
                linkingParentNode = null;
            }
            else
            {
                selectedDialogue.ToggleRelation(linkingParentNode, dialogueNode);
                linkingParentNode = null;
                MarkConnectionsDirty();
            }
            RefreshLinkButtons();
        }

        private void RefreshLinkButtons()
        {
            if (nodeViews == null) { return; }
            foreach (DialogueNodeView view in nodeViews.Values)
            {
                DialogueNode node = view.dialogueNode;
                string label;
                if (linkingParentNode == null) { label = "link"; }
                else if (node == linkingParentNode) { label = "---"; }
                else { label = Dialogue.IsRelated(linkingParentNode, node) ? "unlink" : "child"; }
                view.SetLinkButtonLabel(label);
            }
        }
        #endregion
        
        #region StaticUIBuilders
        private static VisualElement MakeNodesLayer()
        {
            return new VisualElement
            {
                name = "nodes-layer",
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    top = 0
                }
            };
        }

        private static VisualElement MakeCanvas()
        {
            return new VisualElement
            {
                name = "canvas-content",
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    top = 0
                }
            };
        }

        private static VisualElement MakeViewport()
        {
            return new VisualElement
            {
                name = "viewport",
                style =
                {
                    flexGrow = 1,
                    overflow = Overflow.Hidden
                }
            };
        }

        private static Label MakeDialogueNameLabel()
        {
            return new Label
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    paddingLeft = 6,
                    paddingTop = 4,
                    paddingBottom = 4
                }
            };
        }
        #endregion
    }
}
