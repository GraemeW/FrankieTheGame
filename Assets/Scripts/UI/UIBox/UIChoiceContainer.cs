using System.Collections.Generic;
using System.Linq;
using Frankie.Control;
using UnityEngine;

namespace Frankie.Utils.UI
{
    public class UIChoiceContainer : UIChoice, IUIMoveInterceptor
    {
        // Tunables
        [SerializeField] private bool isMoveHorizontal = true;

        // State
        private readonly List<UIChoice> uiChoices = new();
        private UIChoice highlightedChoiceOption;
        
        #region PublicMethods
        public void Add(UIChoice uiChoice)
        {
            uiChoices.Add(uiChoice);
        }

        public IList<UIChoice> GetSubOptions() => uiChoices.ToList();
        
        public bool TryMove(PlayerInputType playerInputType)
        {
            if (uiChoices.Count == 0) { return false; }
            
            switch (playerInputType)
            {
                case PlayerInputType.NavigateLeft when isMoveHorizontal:
                case PlayerInputType.NavigateUp when !isMoveHorizontal:
                    highlightedChoiceOption.Highlight(false);
                    highlightedChoiceOption = GetNextChoice(false);
                    highlightedChoiceOption.Highlight(true);
                    return true;
                case PlayerInputType.NavigateRight when isMoveHorizontal:
                case PlayerInputType.NavigateDown when !isMoveHorizontal:
                    highlightedChoiceOption.Highlight(false);
                    highlightedChoiceOption = GetNextChoice(true);
                    highlightedChoiceOption.Highlight(true);
                    return true;
                default:
                    return false;
            }
        }

        public override void UseChoice()
        {
            if (highlightedChoiceOption == null) { return; }
            highlightedChoiceOption.UseChoice();
        }

        public override void Highlight(bool enable)
        {
            if (uiChoices.Count == 0) { return; }

            highlightedChoiceOption = null;
            foreach (UIChoice uiChoice in uiChoices)
            {
                uiChoice.Highlight(false);
            }

            if (enable)
            {
                highlightedChoiceOption = uiChoices[0];
                highlightedChoiceOption.Highlight(true);
            }
        }
        #endregion
        
        #region PrivateMethods
        private UIChoice GetNextChoice(bool isForward)
        {
            if (uiChoices.Count == 0) { return null; }
            if (highlightedChoiceOption == null || !uiChoices.Contains(highlightedChoiceOption)) { return uiChoices.FirstOrDefault(); }
            
            int choiceIndex = uiChoices.IndexOf(highlightedChoiceOption);
            int newChoiceIndex = isForward ? choiceIndex + 1 : choiceIndex - 1;
            if (newChoiceIndex < 0) { newChoiceIndex = uiChoices.Count - 1; }
            else if (newChoiceIndex >= uiChoices.Count) { newChoiceIndex = 0; }
            
            return uiChoices[newChoiceIndex];
        }
        #endregion
    }
}
