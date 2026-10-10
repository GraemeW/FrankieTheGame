using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.UIBox;
using LowDefMustard.Localization;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    [Serializable]
    public sealed class NamingKeyboard
    {
        // Note:
        //  - Keys per row are worked out from the character counts (see NamingKeyboardLayout)
        //  - Key size is fixed in USS to prevent expansion/overflow w/ character/font size ∆s
        
        // Tunables
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedKeyboardKeys;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedKeyboardKeysUpper;
        [SerializeField] private string additionalSpecialCharacters = "0123456789._-@!*^";
        [SerializeField] private NamingKeyboardLayout layout = new();

        // State
        private readonly List<ChoiceEntryHandle> lowerKeys = new();
        private readonly List<ChoiceEntryHandle> upperKeys = new();
        private readonly List<ChoiceEntryHandle> specialKeys = new();
        private string builtLowerCharacters;
        private string builtUpperCharacters;

        #region PublicMethods
        public IEnumerable<EntryHandle> CreateEntries(UIToolkitBoxView view, Action<char> onKeyChosen, Func<Action, Action> withSelectSound)
        {
            var entryHandles = new List<EntryHandle>();
            CreateLetterKeys(entryHandles, view, onKeyChosen, withSelectSound);
            CreateKeys(entryHandles, additionalSpecialCharacters, typeof(KeyboardSpecialKeys), specialKeys, view, onKeyChosen, withSelectSound);
            return entryHandles;
        }

        // Letters follow the locale:  when its alphabet differs from the one built, the letter keys are replaced (special characters are kept)
        public IEnumerable<EntryHandle> RebuildLetterEntries(UIToolkitBoxView view, Action<char> onKeyChosen, Func<Action, Action> withSelectSound)
        {
            var entryHandles = new List<EntryHandle>();
            if (localizedKeyboardKeys.GetSafeLocalizedString() == builtLowerCharacters && localizedKeyboardKeysUpper.GetSafeLocalizedString() == builtUpperCharacters) { return entryHandles; }

            foreach (ChoiceEntryHandle keyHandle in lowerKeys.Concat(upperKeys)) { keyHandle.Remove(); }
            lowerKeys.Clear();
            upperKeys.Clear();
            CreateLetterKeys(entryHandles, view, onKeyChosen, withSelectSound);
            return entryHandles;
        }

        public void ApplyLayout(NamingPanelModel namingPanelModel) // After CreateEntries
        {
            layout.GetColumns(Mathf.Max(lowerKeys.Count, upperKeys.Count), specialKeys.Count, out int letterColumns, out int specialColumns);
            namingPanelModel.letterColumns = letterColumns;
            namingPanelModel.specialColumns = specialColumns;
        }

        public IEnumerable<IUIChoice> GetKeys(bool isUpper) => (isUpper ? upperKeys : lowerKeys).Concat(specialKeys);

        public IEnumerable<TableEntryReference> GetLocalizationEntries()
        {
            yield return localizedKeyboardKeys.TableEntryReference;
            yield return localizedKeyboardKeysUpper.TableEntryReference;
        }
        #endregion

        #region PrivateMethods
        private void CreateLetterKeys(List<EntryHandle> entryHandles, UIToolkitBoxView view, Action<char> onKeyChosen, Func<Action, Action> withSelectSound)
        {
            builtLowerCharacters = localizedKeyboardKeys.GetSafeLocalizedString();
            builtUpperCharacters = localizedKeyboardKeysUpper.GetSafeLocalizedString();
            CreateKeys(entryHandles, builtLowerCharacters, typeof(KeyboardLowerKeys), lowerKeys, view, onKeyChosen, withSelectSound);
            CreateKeys(entryHandles, builtUpperCharacters, typeof(KeyboardUpperKeys), upperKeys, view, onKeyChosen, withSelectSound);
        }

        private static void CreateKeys(List<EntryHandle> entryHandles, string characters, Type containerType, List<ChoiceEntryHandle> keys, UIToolkitBoxView view, Action<char> onKeyChosen, Func<Action, Action> withSelectSound)
        {
            if (string.IsNullOrEmpty(characters)) { return; }
            foreach (char character in characters)
            {
                var keyHandle = new ChoiceEntryHandle(view, character.ToString(), true, withSelectSound(() => onKeyChosen(character)), null, containerType);
                entryHandles.Add(keyHandle);
                keys.Add(keyHandle);
            }
        }
        #endregion
    }
}
