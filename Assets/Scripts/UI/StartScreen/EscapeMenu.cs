using System;
using UnityEngine;
using Frankie.Control;
using Frankie.Utils.UI;
using Frankie.Core;
using Frankie.World;

namespace Frankie.Menu.UI
{
    public class EscapeMenu : UIBox
    {
        // Tunables
        [SerializeField] private OptionsMenu optionsMenuPrefab;

        // Cached References
        private PlayerStateMachine playerStateMachine;
        private WorldCanvas worldCanvas;
        private GameObject childOption;

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
            
            UnityEngine.Debug.Log(playerInputType);
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
    }
}
