using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using TMPro;
using Frankie.Core;
using Frankie.Control;
using Frankie.Saving;
using Frankie.World;
using Frankie.Utils;
using Frankie.Utils.UI;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    public class EscapeMenu : UIBox, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedEscapeHeaderText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionOptionsText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionQuitText;
        [Header("Hookups")]
        [SerializeField] private TMP_Text escapeHeaderField;
        [SerializeField] private UIChoiceButton optionOptionsField;
        [SerializeField] private UIChoiceButton optionQuitField;
        
        [Header("Prefabs")]
        [SerializeField] private OptionsMenu optionsMenuPrefab;

        // Cached References
        private PlayerStateMachine playerStateMachine;
        private WorldCanvas worldCanvas;
        private GameObject childOption;

        // Events
        public event Action escapeMenuItemSelected;
        
        // UIBox Configuration
        protected override EnumLookupBase<UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var escapeMenuConfiguration = new EnumLookup<UIBoxState,UIBoxStateBehaviour>();
            var defaultStateBehaviour = new UIBoxStateBehaviour( 
                isBackInput: ImplementIsBackInput,
                tryHandleBackNavigation: ImplementTryHandleBackNavigation);
            escapeMenuConfiguration.TrySet(UIBoxState.Default, defaultStateBehaviour);
            return escapeMenuConfiguration;
        }

        #region UnityMethods
        protected override bool TryAcquireDependencies()
        {
            worldCanvas = WorldCanvas.FindWorldCanvas();
            playerStateMachine = Player.FindPlayerStateMachine();
            if (worldCanvas == null || playerStateMachine == null) { return false; }

            controller = playerStateMachine.GetComponent<PlayerController>();
            if (controller == null) { return false; }

            controller.AddInputReceiver(this, null);
            return true;
        }

        protected override void StartTriggered()
        {
            ResetAllTextElements();
        }

        protected override void DestroyTriggered()
        {
            playerStateMachine?.EnterWorld();
        }
        #endregion
        
        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } =  LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedEscapeHeaderText.TableEntryReference,
                localizedOptionOptionsText.TableEntryReference,
                localizedOptionQuitText.TableEntryReference,
            };
        }
        #endregion
        
        #region PublicMethods
        public void ResetAllTextElements()
        {
            if (escapeHeaderField != null) { escapeHeaderField.SetText(localizedEscapeHeaderText.GetSafeLocalizedString()); }
            if (optionOptionsField != null) { optionOptionsField.SetText(localizedOptionOptionsText.GetSafeLocalizedString()); }
            if (optionQuitField != null) { optionQuitField.SetText(localizedOptionQuitText.GetSafeLocalizedString()); }
        }
        
        public void OpenOptionsMenu() // Called via Unity Events
        {
            // Front-load event calling -- despawns any open windows
            escapeMenuItemSelected?.Invoke();

            OptionsMenu optionsMenu = Instantiate(optionsMenuPrefab, worldCanvas.gameObject.transform);
            optionsMenu.Setup(this);
            controller.AddInputReceiver(optionsMenu, null);
        }

        public void QuitGame() // Called via Unity Events
        {
            SavingWrapper.LoadStartScene();
        }
        #endregion
        
        #region InputHandling
        private bool ImplementIsBackInput(ControllerInputType controllerInputType) => controllerInputType is ControllerInputType.Escape or ControllerInputType.Cancel;

        private bool ImplementTryHandleBackNavigation(ControllerInputType controllerInputType)
        {
            if (childOption == null) { return false; }
            Destroy(childOption);
            return true;
        }
        #endregion
    }
}
