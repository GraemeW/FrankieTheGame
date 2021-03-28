using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Control;
using UnityEngine.UI;
using Frankie.Core;
using Frankie.Stats;

namespace Frankie.Speech.UI
{
    public class EscapeMenu : DialogueOptionBox
    {
        // Tunables
        [SerializeField] Button optionsMenuButton = null;
        [SerializeField] Button quitGameMenuButton = null;
        [Header("Option Game Objects")]
        [SerializeField] GameObject optionsMenuPrefab = null;

        // Cached References
        PlayerStateHandler playerStateHandler = null;
        PlayerController playerController = null;
        WorldCanvas worldCanvas = null;
        GameObject childOption = null;

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
        }

        private void OnDestroy()
        {
            playerStateHandler.ExitEscapeMenu();
        }

        public override void HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return; }

            if (playerInputType == PlayerInputType.Cancel)
            {
                if (childOption != null)
                {
                    Destroy(childOption);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            base.HandleGlobalInput(playerInputType);
        }

        public void OpenOptionsMenu() // Called via Unity Events
        {
            handleGlobalInput = false;
            GameObject childOption = Instantiate(optionsMenuPrefab, worldCanvas.gameObject.transform);
            OptionsMenu optionsMenu = childOption.GetComponent<OptionsMenu>();
            optionsMenu.SetGlobalCallbacks(playerController);
            optionsMenu.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
        }

        public void QuitGame() // Called via Unity Events
        {
            Application.Quit();
        }
    }

}
