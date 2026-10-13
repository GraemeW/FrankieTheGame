using System;
using UnityEngine;

namespace Frankie.Menu.UI
{
    [Serializable]
    public sealed class NameInput
    {
        // Tunables
        [SerializeField] private char padCharacter = '·';
        [SerializeField] private int maxSize = 8;

        // State
        private string currentText = "";
        private NamingPanelModel namingPanelModel;

        #region PublicMethods
        public void Initialize(NamingPanelModel setNamingPanelModel)
        {
            namingPanelModel = setNamingPanelModel;
            RefreshDisplay();
        }

        public string GetCurrentText() => currentText;
        public bool HasText() => !string.IsNullOrEmpty(currentText);

        public void Add(char character)
        {
            if (currentText.Length >= maxSize) { return; }
            SetText(currentText + character);
        }

        public void RemoveLast()
        {
            if (currentText.Length == 0) { return; }
            SetText(currentText.Remove(currentText.Length - 1));
        }

        public void Clear() => SetText("");
        public void SetText(string text)
        {
            currentText = text ?? "";
            RefreshDisplay();
        }
        #endregion

        #region PrivateMethods
        private void RefreshDisplay()
        {
            if (namingPanelModel == null) { return; }
            namingPanelModel.inputText = currentText.PadRight(maxSize, padCharacter);
        }
        #endregion
    }
}
