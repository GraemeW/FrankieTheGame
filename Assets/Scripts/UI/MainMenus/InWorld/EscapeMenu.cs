using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Control;
using LowDefMustard.UIBox;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Core;
using Frankie.Control;
using Frankie.Saving;
using Frankie.World;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class EscapeMenu : UIBox<UIBoxState>, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedEscapeHeaderText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionOptionsText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionFlavourText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionQuitText;
        [Header("Prefabs")]
        [SerializeField] private OptionsMenu optionsMenuPrefab;
        [SerializeField] private FrameFlavourPanel frameFlavourPanelPrefab;

        // Cached References
        private PlayerStateMachine playerStateMachine;
        private WorldCanvas worldCanvas;
        private GameObject childOption;

        // State
        private readonly EscapeMenuModel escapeMenuModel = new();
        private IUIChoice optionsChoice;
        private IUIChoice flavourChoice;
        private IUIChoice quitChoice;

        // Events
        public event Action escapeMenuItemSelected;
        
        // UIBox Configuration
        protected override EnumLookup<UIBoxState,UIBoxStateBehaviour> BuildStateBehaviours()
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

        protected override void AwakeTriggered()
        {
            GetComponent<UIToolkitMenuView>().SetDataSource(escapeMenuModel);
            clearVolatileOptionsOnEnable = false;
            BuildChoiceOptions();
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
                localizedOptionFlavourText.TableEntryReference,
                localizedOptionQuitText.TableEntryReference,
            };
        }
        #endregion
        
        #region PublicMethods
        public void ResetAllTextElements()
        {
            escapeMenuModel.headerText = localizedEscapeHeaderText.GetSafeLocalizedString();
            SetChoiceText(optionsChoice, localizedOptionOptionsText);
            SetChoiceText(flavourChoice, localizedOptionFlavourText);
            SetChoiceText(quitChoice, localizedOptionQuitText);
        }
        #endregion

        #region PrivateMethods
        private void OpenOptionsMenu()
        {
            if (optionsMenuPrefab == null) { return; }
            
            // Front-load event calling -- despawns any open windows
            escapeMenuItemSelected?.Invoke();

            OptionsMenu optionsMenu = Instantiate(optionsMenuPrefab, worldCanvas.gameObject.transform);
            optionsMenu.Setup(this);
            controller.AddInputReceiver(optionsMenu, null);
        }

        private void OpenFrameFlavourPanel()
        {
            if  (frameFlavourPanelPrefab == null) { return; }
            
            // Front-load event calling -- despawns any open windows
            escapeMenuItemSelected?.Invoke();
            
            FrameFlavourPanel frameFlavourPanel = Instantiate(frameFlavourPanelPrefab, worldCanvas.gameObject.transform);
            frameFlavourPanel.EnableEscapeOptionExit();
            controller.AddInputReceiver(frameFlavourPanel, null);
        }

        private void QuitGame()
        {
            SaveFileManager.LoadStartScene();
        }

        private void BuildChoiceOptions()
        {
            optionsChoice = AddNonDestroyChoiceOption(localizedOptionOptionsText.GetSafeLocalizedString(), OpenOptionsMenu);
            flavourChoice = AddNonDestroyChoiceOption(localizedOptionFlavourText.GetSafeLocalizedString(), OpenFrameFlavourPanel);
            AddSeparator();
            quitChoice = AddNonDestroyChoiceOption(localizedOptionQuitText.GetSafeLocalizedString(), QuitGame);
        }

        private static void SetChoiceText(IUIChoice choice, LocalizedString localizedString)
        {
            if (!IsChoiceAlive(choice)) { return; }
            choice.SetText(localizedString.GetSafeLocalizedString());
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
