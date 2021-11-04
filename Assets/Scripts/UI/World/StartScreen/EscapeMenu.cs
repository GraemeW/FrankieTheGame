using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Control;
using System;
using Frankie.Utils.UI;
using Frankie.Core;

namespace Frankie.Menu.UI
{
    public class EscapeMenu : UIBox
    {
        // Tunables
        [SerializeField] GameObject optionsMenuPrefab = null;

        // Cached References
        SavingWrapper savingWrapper = null;
        PlayerStateHandler playerStateHandler = null;
        WorldCanvas worldCanvas = null;
        GameObject childOption = null;

        // Events
        public event Action escapeMenuItemSelected;

        private void Awake()
        {
            worldCanvas = GameObject.FindGameObjectWithTag("WorldCanvas").GetComponent<WorldCanvas>();
            playerStateHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStateHandler>();
            controller = playerStateHandler.GetComponent<PlayerController>();
        }

        private void Start()
        {
            savingWrapper = GameObject.FindGameObjectWithTag("Saver").GetComponent<SavingWrapper>();
            // SceneLoader is a persistent object, thus can only be found after Awake -- so find in Start

            TakeControl(controller, this, null); // input handled via player controller, immediate override
            HandleClientEntry();
        }

        private void OnDestroy()
        {
            playerStateHandler.ExitEscapeMenu();
        }

        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return true; } // Spoof:  Cannot accept input, so treat as if global input already handled

            if (playerInputType == PlayerInputType.Option || playerInputType == PlayerInputType.Cancel)
            {
                if (childOption != null)
                {
                    Destroy(childOption);
                    return true;
                }
            }

            return base.HandleGlobalInput(playerInputType);
        }

        public void OpenOptionsMenu() // Called via Unity Events
        {
            // Frontload event calling -- despawns any open windows
            if (escapeMenuItemSelected != null)
            {
                escapeMenuItemSelected.Invoke();
            }

            GameObject childOption = Instantiate(optionsMenuPrefab, worldCanvas.gameObject.transform);
            OptionsMenu optionsMenu = childOption.GetComponent<OptionsMenu>();
            optionsMenu.Setup(this);
            PassControl(optionsMenu);
        }

        public void QuitGame() // Called via Unity Events
        {
            savingWrapper.LoadStartMenu();
        }
    }

}
