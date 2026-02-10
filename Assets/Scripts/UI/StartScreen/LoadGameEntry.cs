using System;
using UnityEngine;
using TMPro;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    public class LoadGameEntry : UIChoiceButton
    {
        // Tunables
        [SerializeField] private TMP_Text indexField;
        [SerializeField] private TMP_Text characterNameField;
        [SerializeField] private TMP_Text levelField;

        public void Setup(int index, string characterName, int level, Action action)
        {
            if (action == null) { return; }

            indexField.text = index.ToString();
            characterNameField.text = characterName;
            levelField.text = level.ToString();
            button.onClick.AddListener(action.Invoke);
        }
    }
}
