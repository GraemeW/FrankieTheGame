using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    public class LoadGameEntry : UIChoiceOption
    {
        // Tunables
        [SerializeField] TMP_Text indexField = null;
        [SerializeField] TMP_Text characterNameField = null;
        [SerializeField] TMP_Text levelField = null;

        public void Setup(int index, string characterName, int level, Action action)
        {
            if (action == null) { return; }

            indexField.text = index.ToString();
            characterNameField.text = characterName;
            levelField.text = level.ToString();
            button.onClick.AddListener(delegate { action.Invoke(); });
        }
    }
}
