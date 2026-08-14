using UnityEngine;
using UnityEngine.UI;
using LowDefMustard.Control;
using LowDefMustard.UIBox;
using LowDefMustard.Utils;

namespace Frankie.Speech.UI
{
    public class DialogueBox : TextScanBox
    {
        // Tunables
        [SerializeField] private bool reconfigureLayoutOnOptionSize = true;
        
        // State -- Option Field Configurables
        private RectOffset optionPadding;
        private float optionSpacing;
        private TextAnchor optionChildAlignment;
        private bool optionControlChildSize = true;
        private bool optionUseChildScale = true;
        private bool optionChildForceExpand;
        
        // Cached References
        protected DialogueController dialogueController;
        
        // UIBox Configuration
        protected override EnumLookup<UIBoxState,UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var dialogueBoxConfiguration = new EnumLookup<UIBoxState,UIBoxStateBehaviour>();
            var defaultStateBehaviour = new UIBoxStateBehaviour( 
                prepareChooseAction: DialoguePrepareChooseAction,
                choose: DialogueChoose, 
                handleGlobalInput: DialogueHandleGlobalInput);
            dialogueBoxConfiguration.TrySet(UIBoxState.Default, defaultStateBehaviour);
            return dialogueBoxConfiguration;
        }
        
        #region UnityMethods
        protected override void AwakeTriggered()
        {
            // Note: A missing DialogueController here is NOT fatal - possible to instantiate a DialogueBox and configure it by Start() check
            
            base.AwakeTriggered();
            dialogueController = DialogueController.FindDialogueController();
            if (dialogueController != null)
            {
                controller = dialogueController;
                controller.AddInputReceiver(this, null);
            }
            StoreOptionPanelConfigurables();
        }

        protected override void EnableTriggered()
        {
            base.EnableTriggered();
            if (dialogueController != null)
            {
                dialogueController.SubscribeToDialogueInput(true, HandleDialogueInput);
                dialogueController.triggerUIUpdates += UpdateUI;
            }
        }

        protected override void DisableTriggered()
        {
            base.DisableTriggered();
            if (dialogueController != null)
            {
                dialogueController.SubscribeToDialogueInput(false, HandleDialogueInput);
                dialogueController.triggerUIUpdates -= UpdateUI;
            }
        }

        protected override void DestroyTriggered()
        {
            base.DestroyTriggered();
            
            // Note 1:  This MUST be called during destruction itself (and not e.g. immediately before in LateUpdate())
            //          Otherwise end-of-dialogue and choice-select options will not trigger
            // Note 2:  Since this is being called in OnDestroy(), all of THIS dialogueBox's handlers are unsubscribed
            //          We try to end conversation, but if another box is subscribed to the controller, conversation continues
            if (dialogueController == null) { return; }
            dialogueController.EndConversation();
        }
        #endregion
        
        #region SetupUpdateMethods
        public override void Setup(string text)
        {
            if (dialogueController != null && dialogueController.IsSimpleMessage())
            {
                AddText(dialogueController.GetSimpleMessage());
                return;
            }
            base.Setup(text);
        }
        
        private void StoreOptionPanelConfigurables()
        {
            if (optionParent == null) { return; }
            if (optionParent.TryGetComponent(out HorizontalLayoutGroup horizontalLayoutGroup))
            {
                optionPadding = horizontalLayoutGroup.padding;
                optionSpacing = horizontalLayoutGroup.spacing;
                optionChildAlignment = horizontalLayoutGroup.childAlignment;

                optionControlChildSize = horizontalLayoutGroup.childControlWidth;
                optionUseChildScale = horizontalLayoutGroup.childScaleWidth;
                optionChildForceExpand = horizontalLayoutGroup.childForceExpandWidth;
            }
        }
        
        private void UpdateUI()
        {
            if (controller == null) { destroyQueued = true; }
            if (!dialogueController.IsActive()) { destroyQueued = true; }

            ClearOldDialogue();
            SetText();
            if (dialogueController.IsChoosing())
            {
                SetChoiceList();
            }
        }

        protected override void OnBusyWriting(bool enable)
        {
            if (enable)
            {
                if (dialogueController != null) { dialogueController.triggerUIUpdates -= UpdateUI; } // unsubscribe from updates - prevent dialogue controller moving on while writing
            }
            else
            {
                if (dialogueController != null) { dialogueController.triggerUIUpdates += UpdateUI; }
            }
        }

        private void SetText()
        {
            if (dialogueController.GetCurrentSpeakerType() == SpeakerType.PlayerSpeaker || dialogueController.GetCurrentSpeakerType() == SpeakerType.AISpeaker)
            {
                AddText(dialogueController.GetCurrentSpeakerName() + ":");
                AddSpeech(dialogueController.GetText());
            }
            else if (dialogueController.GetCurrentSpeakerType() == SpeakerType.NarratorDirection)
            {
                AddSpeech(dialogueController.GetText());
            }
        }
        #endregion
        
        #region ChoiceFunctionality
        private void SetChoiceList()
        {
            int choiceIndex = 0;
            int maxChoiceLength = 0;
            foreach (DialogueNode choiceNode in dialogueController.GetChoices())
            {
                AddChoice(choiceNode, choiceIndex);
                maxChoiceLength = Mathf.Max(maxChoiceLength, choiceNode.GetText().Length);
                choiceIndex++;
            }

            ConfigureChoiceLayout(choiceIndex, maxChoiceLength);
        }

        protected void ConfigureChoiceLayout(int choiceCount, int maxChoiceLength)
        {
            if (!reconfigureLayoutOnOptionSize || choiceCount == 0) { return; }

            if (choiceCount > DialogueController.GetChoiceNumberThresholdToReconfigureVertical() || maxChoiceLength > DialogueController.GetChoiceLengthThresholdToReconfigureVertical())
            {
                if (optionParent.TryGetComponent(out HorizontalLayoutGroup horizontalLayoutGroup)) { DestroyImmediate(horizontalLayoutGroup); }
                if (!optionParent.TryGetComponent(out VerticalLayoutGroup verticalLayoutGroup))
                {
                    verticalLayoutGroup = optionParent.gameObject.AddComponent(typeof(VerticalLayoutGroup)) as VerticalLayoutGroup;
                    if (verticalLayoutGroup == null) { return; }
                    
                    verticalLayoutGroup.padding = optionPadding;
                    verticalLayoutGroup.spacing = optionSpacing;
                    verticalLayoutGroup.childAlignment = optionChildAlignment;
                    verticalLayoutGroup.childControlWidth = optionControlChildSize;
                    verticalLayoutGroup.childControlHeight = optionControlChildSize;
                    verticalLayoutGroup.childScaleWidth = optionUseChildScale;
                    verticalLayoutGroup.childScaleHeight = optionUseChildScale;
                    verticalLayoutGroup.childForceExpandWidth = optionChildForceExpand;
                    verticalLayoutGroup.childForceExpandHeight = optionChildForceExpand;
                }
            }
            else
            {
                if (optionParent.TryGetComponent(out VerticalLayoutGroup verticalLayoutGroup)) { DestroyImmediate(verticalLayoutGroup); }
                if (!optionParent.TryGetComponent(out HorizontalLayoutGroup horizontalLayoutGroup))
                {
                    horizontalLayoutGroup = optionParent.gameObject.AddComponent(typeof(HorizontalLayoutGroup)) as HorizontalLayoutGroup;
                    if (horizontalLayoutGroup == null) { return; }
                    
                    horizontalLayoutGroup.padding = optionPadding;
                    horizontalLayoutGroup.spacing = optionSpacing;
                    horizontalLayoutGroup.childAlignment = optionChildAlignment;
                    horizontalLayoutGroup.childControlWidth = optionControlChildSize;
                    horizontalLayoutGroup.childControlHeight = optionControlChildSize;
                    horizontalLayoutGroup.childScaleWidth = optionUseChildScale;
                    horizontalLayoutGroup.childScaleHeight = optionUseChildScale;
                    horizontalLayoutGroup.childForceExpandWidth = optionChildForceExpand;
                    horizontalLayoutGroup.childForceExpandHeight = optionChildForceExpand;
                }
            }
        }

        private void AddChoice(DialogueNode choiceNode, int choiceIndex = 0)
        {
            GameObject dialogueChoiceOptionObject = Instantiate(optionButtonPrefab, optionParent);
            var dialogueChoiceOption = dialogueChoiceOptionObject.GetComponent<DialogueChoiceOption>();
            dialogueChoiceOption.Setup(dialogueController, choiceNode);
            dialogueChoiceOption.SetChoiceOrder(choiceIndex);
            dialogueChoiceOption.SetText(choiceNode.GetText());
            dialogueChoiceOption.AddOnClickListener(delegate { Choose(choiceNode.name); });
            dialogueChoiceOption.gameObject.SetActive(false);

            QueueTextForPrinting(dialogueChoiceOption.gameObject, null, true);
        }

        private bool DialogueChoose(string nodeID)
        {
            if (!UsesNodeBasedDialogueFlow()) { return StandardChoose(nodeID); }

            bool choose = PrepareChooseAction(ControllerInputType.Execute);
            if (choose)
            {
                TriggerUIBoxModified(ReceiverModifiedType.ItemSelected, new ReceiverModifiedData(this));
                dialogueController.NextWithID(nodeID);
            }
            return choose;
        }

        private bool DialoguePrepareChooseAction(ControllerInputType controllerInputType)
        {
            if (controllerInputType != ControllerInputType.Execute) { return false; }
            if (TryFastForwardActiveText()) { return true; }
            if (!UsesNodeBasedDialogueFlow()) { return StandardPrepareChooseAction(controllerInputType); }

            // Note:  Node-based selection handled via:
            // keyboard -> DialogueController.InteractWithChoices -> NextWithID,
            // mouse -> choice button's click listener -> Choose(nodeID)
            return true;
        }
        #endregion
        
        #region InputHandling
        protected virtual bool UsesNodeBasedDialogueFlow() => true;
        // true (default): DialogueBox branching dialogue - DialogueController owns cursor/choice input
        // false: generic choice presentation (e.g. DialogueOptionBox) -- defers to UIBox pipeline

        private bool DialogueHandleGlobalInput(ControllerInputType controllerInputType)
        {
            if (isInitialInputBlocked) { return false; }
            if (!UsesNodeBasedDialogueFlow()) { return StandardHandleGlobalInput(controllerInputType); }
            
            if (!handleGlobalInput) { return true; }
            if (TryEarlyExit(controllerInputType)) { return true; }
            if (controllerInputType == ControllerInputType.Execute)
            {
                if (isWriting) { SkipToEndOfPage(); return true; }
                if (dialogueController != null && !dialogueController.IsSimpleMessage())
                {
                    return true;  // dialogue completion handled by dialogue controller
                }

                if (!IsChoiceAvailable())
                {
                    destroyQueued = true; // otherwise queue for deletion on click through
                    return true;
                }
            }
            return false;
        }
        
        private void HandleDialogueInput(ControllerInputType controllerInputType)
        {
            PrepareChooseAction(controllerInputType);
        }
        #endregion
    }
}
