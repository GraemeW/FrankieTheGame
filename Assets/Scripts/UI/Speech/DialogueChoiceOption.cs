using Frankie.Utils.UI;

namespace Frankie.Speech.UI
{
    public class DialogueChoiceOption : UIChoiceButton
    {
        // State
        private DialogueNode dialogueNode;

        // Cached References
        private DialogueController dialogueController;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (dialogueController != null)
            {
                dialogueController.highlightedNodeChanged -= Highlight;
            }
        }

        public void Setup(DialogueController setDialogueController, DialogueNode setDialogueNode)
        {
            dialogueController = setDialogueController;
            dialogueNode = setDialogueNode;
            setDialogueController.highlightedNodeChanged += Highlight;
        }

        private void Highlight(DialogueNode dialogueNodeToHighlight)
        {
            if (dialogueNode == dialogueNodeToHighlight)
            {
                selectionMarker.SetActive(true);
                itemHighlighted?.Invoke();
            }
            else
            {
                selectionMarker.SetActive(false);
            }
        }
    }
}
