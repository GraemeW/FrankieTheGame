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
        [SerializeField] OptionsMenu optionsMenuPrefab = null;

        // Cached References
        SavingWrapper savingWrapper = null;
        PlayerStateMachine playerStateHandler = null;
        WorldCanvas worldCanvas = null;
        GameObject childOption = null;

        // Events
        public event Action escapeMenuItemSelected;

        private void Awake()
        {
            worldCanvas = GameObject.FindGameObjectWithTag("WorldCanvas")?.GetComponent<WorldCanvas>();
            playerStateHandler = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerStateMachine>();
            if (worldCanvas == null || playerStateHandler == null) { Destroy(gameObject); }

            controller = playerStateHandler?.GetComponent<PlayerController>();
        }

        private void Start()
        {
            savingWrapper = GameObject.FindGameObjectWithTag("Saver")?.GetComponent<SavingWrapper>();
            // SceneLoader is a persistent object, thus can only be found after Awake -- so find in Start

            HandleClientEntry();
        }

        private void OnDestroy()
        {
            playerStateHandler?.EnterWorld();
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
            escapeMenuItemSelected?.Invoke();

            OptionsMenu optionsMenu = Instantiate(optionsMenuPrefab, worldCanvas.gameObject.transform);
            optionsMenu.Setup(this);
            PassControl(optionsMenu);
        }

        public void QuitGame() // Called via Unity Events
        {
            SavingWrapper.LoadStartScene();
        }
    }

}
