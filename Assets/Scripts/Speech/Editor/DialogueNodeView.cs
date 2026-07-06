using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Frankie.Stats;

namespace Frankie.Speech.Editor
{
    public class DialogueNodeView : VisualElement
    {
        // Const Tunables
        private const float _headerHeight = 28f;
        private const float _textFieldMinHeight = 100f;
        private const float _nodePaddingHorizontal = 20f;
        private const float _nodePaddingVertical = 16f;
        private const float _cornerRadius = 6f;
        private static readonly Color _headerColor = new(0f, 0f, 0f, 0.5f);
        private static readonly Color _playerSpeakerColor = Color.darkOliveGreen;
        private static readonly Color _defaultColor = Color.gray2;
        private static readonly Color[] _aiSpeakerColors = { Color.darkOrange * 0.85f, Color.softYellow * 0.75f, Color.lightPink * 0.75f, Color.mediumPurple * 0.85f };

        // State
        private readonly Dialogue dialogue;
        public DialogueNode dialogueNode { get; }

        // UI State
        private readonly Label idLabel;
        private readonly TextField speakerNameField;
        private readonly Button linkButton;
        private readonly Button deleteButton;

        public event Action<DialogueNode, string> speakerNameChanged;
        public event Action<DialogueNode, SpeakerType> speakerTypeChanged;
        public event Action<DialogueNode, string> textChanged;
        public event Action<DialogueNode> deleteRequested;
        public event Action<DialogueNode> createChildRequested;
        public event Action<DialogueNode> linkClicked;

        public DialogueNodeView(DialogueNode dialogueNode, Dialogue dialogue, Action onPositionChanged, Func<float> zoomProvider)
        {
            this.dialogueNode = dialogueNode;
            this.dialogue = dialogue;

            InitializeNodeStyle();

            VisualElement nodeHeader = MakeNodeHeader();
            nodeHeader.AddManipulator(new DialogueNodeDragManipulator(this, dialogueNode, onPositionChanged, zoomProvider));
            Add(nodeHeader);

            idLabel = new Label();
            nodeHeader.Add(idLabel);
            
            var headerSpacer = new VisualElement { style = { height = 10 } };
            Add(headerSpacer);

            var speakerRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            Add(speakerRow);
            
            speakerNameField = MakeSpeakerTextField();
            speakerNameField.RegisterValueChangedCallback(OnSpeakerNameFieldChanged);
            speakerRow.Add(speakerNameField);

            var speakerRowSpacer = new VisualElement { style = { flexGrow = 1 } };
            speakerRow.Add(speakerRowSpacer);
            
            var speakerTypeField = new EnumField(dialogueNode.GetSpeakerType());
            speakerTypeField.RegisterValueChangedCallback(OnSpeakerTypeFieldChanged);
            speakerRow.Add(speakerTypeField);

            TextField textField = MakeTextTextField();
            textField.RegisterValueChangedCallback(OnTextFieldChanged);
            Add(textField);

            var cardSpacer = new VisualElement { style = { flexGrow = 1 } };
            Add(cardSpacer);

            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            Add(buttonRow);
            
            linkButton = MakeLinkButton();
            linkButton.RegisterCallback<ClickEvent>(_ => linkClicked?.Invoke(dialogueNode));
            buttonRow.Add(linkButton);
            
            var buttonSpacer = new VisualElement { style = { flexGrow = 1 } };
            buttonRow.Add(buttonSpacer);
            
            deleteButton = MakeAddRemoveButton(false);
            deleteButton.RegisterCallback<ClickEvent>(_ => deleteRequested?.Invoke(dialogueNode));
            buttonRow.Add(deleteButton);
            
            Button createButton = MakeAddRemoveButton(true);
            createButton.RegisterCallback<ClickEvent>(_ => createChildRequested?.Invoke(dialogueNode));
            buttonRow.Add(createButton);

            
            speakerNameField.SetValueWithoutNotify(dialogueNode.GetSpeakerName(false));
            textField.SetValueWithoutNotify(dialogueNode.GetText());

            RefreshVisualStyle();
        }

        #region PublicMethods
        public void SetSpeakerNameFieldValue(string value) => speakerNameField.SetValueWithoutNotify(value);
        public void SetLinkButtonLabel(string label) => linkButton.text = label;
        
        public void RefreshVisualStyle()
        {
            Rect rect = dialogueNode.GetRect();
            style.left = rect.x;
            style.top = rect.y;
            style.width = rect.width;
            style.height = rect.height;

            idLabel.text = $"--Unique ID: {dialogueNode.name}--";
            style.backgroundColor = ResolveBackgroundColor(dialogueNode, dialogue);
            deleteButton.style.display = dialogueNode == dialogue.GetRootNode() ? DisplayStyle.None : DisplayStyle.Flex;
        }
        #endregion

        #region EventHandling
        private void OnSpeakerNameFieldChanged(ChangeEvent<string> evt) => speakerNameChanged?.Invoke(dialogueNode, evt.newValue);
        private void OnSpeakerTypeFieldChanged(ChangeEvent<Enum> evt) => speakerTypeChanged?.Invoke(dialogueNode, (SpeakerType)evt.newValue);
        private void OnTextFieldChanged(ChangeEvent<string> evt) => textChanged?.Invoke(dialogueNode, evt.newValue);
        #endregion
        
        #region UIStyleMakers
        private void InitializeNodeStyle()
        {
            style.position = Position.Absolute;
            style.paddingLeft = _nodePaddingHorizontal;
            style.paddingRight = _nodePaddingHorizontal / 2f;
            style.paddingTop = _nodePaddingVertical / 2f;
            style.paddingBottom = _nodePaddingVertical;
            style.borderTopLeftRadius = _cornerRadius;
            style.borderTopRightRadius = _cornerRadius;
            style.borderBottomLeftRadius = _cornerRadius;
            style.borderBottomRightRadius = _cornerRadius;
            style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
        }
        
        private static VisualElement MakeNodeHeader()
        {
            return new VisualElement
            {
                name = "drag-header",
                style =
                {
                    height = _headerHeight,
                    marginLeft = -_nodePaddingHorizontal,
                    marginRight = -_nodePaddingHorizontal / 2f,
                    marginTop = -_nodePaddingVertical / 2f,
                    backgroundColor = _headerColor,
                    justifyContent = Justify.Center
                }
            };
        }
        
        private static TextField MakeSpeakerTextField()
        {
            return new TextField("Speaker:")
            {
                isDelayed = true,
                style = { flexGrow = 1 },
                labelElement = { style = { minWidth = 50, width = 50} }
            };
        }
        
        private static TextField MakeTextTextField()
        {
            return new TextField
            {
                multiline = true, isDelayed = true,
                style =
                {
                    minHeight = _textFieldMinHeight,
                    marginTop = 4
                }
            };
        }
        
        private static Button MakeLinkButton()
        {
            return new Button()
            {
                text = "link",
                style =
                {
                    width = 100
                }
            };
        }

        private static Button MakeAddRemoveButton(bool add)
        {
            return new Button()
            {
                text = add ? "+" : "-",
                style =
                {
                    width = 50
                }
            };
        }

        private static Color ResolveBackgroundColor(DialogueNode dialogueNode, Dialogue dialogue)
        {
            switch (dialogueNode.GetSpeakerType())
            {
                case SpeakerType.PlayerSpeaker:
                    return _playerSpeakerColor;
                case SpeakerType.AISpeaker:
                    return ResolveAISpeakerColor(dialogueNode, dialogue);
                case SpeakerType.NarratorDirection:
                default:
                    return _defaultColor;
            }
        }

        private static Color ResolveAISpeakerColor(DialogueNode dialogueNode, Dialogue dialogue)
        {
            List<CharacterProperties> activeSpeakers = dialogue.GetActiveCharacters();
            for (int i = 0; i < activeSpeakers.Count; i++)
            {
                if (activeSpeakers[i] != dialogueNode.GetCharacterProperties()) { continue; }
                if (i < _aiSpeakerColors.Length) { return _aiSpeakerColors[i]; }
                break;
            }
            return _defaultColor;
        }
        #endregion
    }
}
