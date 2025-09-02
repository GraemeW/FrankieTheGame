using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Frankie.Utils.UI
{
    public class UIChoiceButton : UIChoice
    {
        [SerializeField] protected Button button = null;

        protected override void OnDestroy()
        {
            button.onClick.RemoveAllListeners();
            base.OnDestroy();
        }

        public Button GetButton()
        {
            return button;
        }
        
        public void AddOnClickListener(UnityAction unityAction)
        {
            if (unityAction == null) { return; }

            button.onClick.AddListener(unityAction);
        }
    }
}
