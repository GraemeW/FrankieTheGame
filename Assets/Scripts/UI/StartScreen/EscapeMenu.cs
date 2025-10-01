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
        PlayerStateMachine playerStateMachine = null;
        WorldCanvas worldCanvas = null;
        GameObject childOption = null;

        // Events
        public event Action escapeMenuItemSelected;

        private void Awake()
        {
            worldCanvas = WorldCanvas.FindWorldCanvas();
            playerStateMachine = Player.FindPlayerStateMachine();
            if (worldCanvas == null || playerStateMachine == null) { Destroy(gameObject); }

            controller = playerStateMachine?.GetComponent<PlayerController>();
        }

        private void Start()
        {
            HandleClientEntry();
        }

        private void OnDestroy()
        {
            playerStateMachine?.EnterWorld();
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
