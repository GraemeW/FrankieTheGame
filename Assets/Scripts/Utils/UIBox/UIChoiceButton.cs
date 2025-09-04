using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Frankie.Utils.UI
{
    public class UIChoiceButton : UIChoice
    {
        // Tunables
        [SerializeField] protected Button button = null;

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
        public void AddOnClickListener(UnityAction unityAction)
        {
            if (unityAction == null) { return; }

            button.onClick.AddListener(unityAction);
        }
        #endregion
    }
}
