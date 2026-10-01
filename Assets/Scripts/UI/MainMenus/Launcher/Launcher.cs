using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using LowDefMustard.UIBox;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Saving;
using Frankie.Stats;

namespace Frankie.Menu.UI
{
    // Scene-level menus (start, game over, game win):  UI Toolkit only, input via MainMenuController
    public abstract class Launcher : UIBox<UIBoxState>
    {
        // Tunables
        [Header("Launcher")]
        [SerializeField] private CharacterProperties nameCharacterProperties;

        // State
        private readonly List<(IUIChoice choice, LocalizedString localizedText)> localizedChoices = new();

        // Cached References
        protected Canvas startCanvas { get; private set; }

        // Abstract
        protected abstract LauncherModel launcherModel { get; }
        protected abstract void BuildChoiceOptions();
        protected abstract void ResetMenuText();

        // UIBox Configuration
        protected override EnumLookup<UIBoxState, UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var launcherConfiguration = new EnumLookup<UIBoxState, UIBoxStateBehaviour>();
            launcherConfiguration.TrySet(UIBoxState.Default, new UIBoxStateBehaviour(setupChoiceOptions: ImplementSetUpChoiceOptions));
            return launcherConfiguration;
        }

        #region UnityMethods
        protected override void AwakeTriggered()
        {
            clearVolatileOptionsOnEnable = false;
            preventEscapeOptionExit = true;
            if (TryGetBoxView(out IUIBoxView view)) { view.SetDataSource(launcherModel); }
        }

        protected override void StartTriggered()
        {
            ResetAllTextElements();
        }
        #endregion

        #region PublicMethods
        public void Setup(Canvas setStartCanvas)
        {
            startCanvas = setStartCanvas;

            // Toggle to set up global input handling
            gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        public void ResetAllTextElements()
        {
            launcherModel.nameText = CharacterProperties.GetCharacterDisplayName(nameCharacterProperties);
            foreach ((IUIChoice choice, LocalizedString localizedText) in localizedChoices)
            {
                if (IsChoiceAlive(choice)) { choice.SetText(localizedText.GetSafeLocalizedString()); }
            }
            ResetMenuText();
        }
        #endregion

        #region ProtectedMethods
        protected void AddLocalizedMenuChoice(LocalizedString localizedText, Action action)
        {
            IUIChoice choice = AddNonDestroyChoiceOption(localizedText.GetSafeLocalizedString(), action);
            localizedChoices.Add((choice, localizedText));
        }

        protected void ReloadStartScreen()
        {
            SetActiveInput(false);
            SaveFileManager.LoadStartScene();
        }
        #endregion

        #region PrivateMethods
        private void ImplementSetUpChoiceOptions()
        {
            if (choiceOptions.Count == 0) { BuildChoiceOptions(); }
            ReconcileChoiceOptions();
        }
        #endregion
    }
}
