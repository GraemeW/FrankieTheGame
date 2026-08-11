using UnityEngine;
using UnityEngine.Events;

namespace LowDefMustard.UIBox
{
    public class UIBackExit : MonoBehaviour
    {
        [SerializeField] private UIChoiceButton backExitButton;

        public void SetBackExitClickBehaviour(UnityAction onBackExitButtonClick)
        {
            if (backExitButton == null) { return; }
            
            backExitButton.AddOnClickListener(onBackExitButtonClick);
        }
    }
}
