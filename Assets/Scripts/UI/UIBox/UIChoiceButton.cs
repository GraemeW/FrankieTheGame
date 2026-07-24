using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Frankie.Utils.UI
{
    public class UIChoiceButton : UIChoice
    {
        // Tunables
        [SerializeField] protected Button button;

        #region UnityMethods
        protected override void OnDestroy()
        {
            button.onClick.RemoveAllListeners();
            base.OnDestroy();
        }
        #endregion

        #region ClassMethods
        public override void UseChoice()
        {
            button.onClick.Invoke();
        }
        #endregion

        #region PublicMethods
        public void DisableOnClickListeners()
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
            }
        }
        
        public void AddOnClickListener(UnityAction unityAction)
        {
            if (unityAction == null) { return; }
            button.onClick.AddListener(unityAction);
        }
        #endregion
    }
}
