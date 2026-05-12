using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using TMPro;
using Frankie.Control;
using Frankie.Utils.UI;
using Frankie.Core;
using Frankie.Utils.Localization;
using Frankie.World;

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

        #region UnityMethods
        private void Awake()
        {
            worldCanvas = WorldCanvas.FindWorldCanvas();
            playerStateMachine = Player.FindPlayerStateMachine();
            if (worldCanvas == null || playerStateMachine == null) { Destroy(gameObject); }

            controller = playerStateMachine?.GetComponent<PlayerController>();
        }

        private void Start()
        {
            if (escapeHeaderField != null) { escapeHeaderField.SetText(localizedEscapeHeaderText.GetSafeLocalizedString()); }
            if (optionOptionsField != null) { optionOptionsField.SetText(localizedOptionOptionsText.GetSafeLocalizedString()); }
            if (optionQuitField != null) { optionQuitField.SetText(localizedOptionQuitText.GetSafeLocalizedString()); }
            HandleClientEntry();
        }

        private void OnDestroy()
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
        public void OpenOptionsMenu() // Called via Unity Events
        {
            // Front-load event calling -- despawns any open windows
            escapeMenuItemSelected?.Invoke();

            OptionsMenu optionsMenu = Instantiate(optionsMenuPrefab, worldCanvas.gameObject.transform);
            optionsMenu.Setup(this);
            PassControl(optionsMenu);
        }

        public void QuitGame() // Called via Unity Events
        {
            SavingWrapper.LoadStartScene();
        }
        #endregion
        
        #region InputHandling
        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return true; } // Spoof:  Cannot accept input, so treat as if global input already handled
            
            if (playerInputType is PlayerInputType.Escape or PlayerInputType.Cancel)
            {
                if (childOption != null)
                {
                    Destroy(childOption);
                    return true;
                }
            }

            return base.HandleGlobalInput(playerInputType);
        }
        #endregion
    }
}
