using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Control;
using UnityEngine.UI;
using Frankie.Core;
using Frankie.Stats;
using System;

namespace Frankie.Speech.UI
{
    public class EscapeMenu : DialogueOptionBox
    {
        // Tunables
        [SerializeField] GameObject optionsMenuPrefab = null;

        // Cached References
        PlayerStateHandler playerStateHandler = null;
        PlayerController playerController = null;
        WorldCanvas worldCanvas = null;
        GameObject childOption = null;

        // Events
        public event Action escapeMenuItemSelected;

        protected override void Awake()
        {
            base.Awake();
            worldCanvas = GameObject.FindGameObjectWithTag("WorldCanvas").GetComponent<WorldCanvas>();
            playerStateHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStateHandler>();
            playerController = playerStateHandler.GetComponent<PlayerController>();
        }

        protected override void Start()
        {
            SetGlobalCallbacks(playerController); // input handled via player controller, immediate override
            HandleClientEntry();
        }

        private void OnDestroy()
        {
            playerStateHandler.ExitEscapeMenu();
        }

        public override void HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return; }

            if (playerInputType == PlayerInputType.Option || playerInputType == PlayerInputType.Cancel)
            {
                if (childOption != null)
                {
                    Destroy(childOption);
                }
                else
                {
                    HandleClientExit();
                    Destroy(gameObject);
                }
            }
            base.HandleGlobalInput(playerInputType);
        }

        public void OpenOptionsMenu() // Called via Unity Events
        {
            // Frontload event calling -- despawns any open windows
            if (escapeMenuItemSelected != null)
            {
                escapeMenuItemSelected.Invoke();
            }

            handleGlobalInput = false;
            GameObject childOption = Instantiate(optionsMenuPrefab, worldCanvas.gameObject.transform);
            OptionsMenu optionsMenu = childOption.GetComponent<OptionsMenu>();
            optionsMenu.Setup(this);
            optionsMenu.SetGlobalCallbacks(playerController);
            optionsMenu.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
        }

        public void QuitGame() // Called via Unity Events
        {
            Application.Quit();
        }
    }

}
