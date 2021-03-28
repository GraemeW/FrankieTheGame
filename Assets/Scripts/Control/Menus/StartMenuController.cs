using Frankie.ZoneManagement;
using Frankie.Speech.UI;
using System;
using UnityEngine;

namespace Frankie.Control
{
    public class StartMenuController : MonoBehaviour, IStandardPlayerInputCaller
    {
        // Tunables
        [Header("Input Parameters")]
        [SerializeField] string interactButtonOne = "Fire1";
        [SerializeField] string interactButtonTwo = "Fire2";
        [SerializeField] KeyCode interactKeyOne = KeyCode.E;
        [SerializeField] string interactCancelButton = "Cancel";
        [SerializeField] KeyCode interactUp = KeyCode.W;
        [SerializeField] KeyCode interactLeft = KeyCode.A;
        [SerializeField] KeyCode interactRight = KeyCode.D;
        [SerializeField] KeyCode interactDown = KeyCode.S;
        [Header("Links and Prefabs")]
        [SerializeField] Canvas startCanvas = null;
        [SerializeField] GameObject optionsPrefab = null;

        // Cached References
        SceneLoader sceneLoader = null;

        // Events
        public event Action<PlayerInputType> globalInput;

        private void Start()
        {
            sceneLoader = GameObject.FindGameObjectWithTag("SceneLoader").GetComponent<SceneLoader>();
            // SceneLoader is a persistent object, thus can only be found after Awake -- so find in Start
        }

        private void Update()
        {
            PlayerInputType playerInputType = GetPlayerInput();
            if (globalInput != null)
            {
                globalInput.Invoke(playerInputType);
            }
        }

        public void NewGame()
        {
            sceneLoader.QueueNewGame();
        }

        public void LoadOptions()
        {
            GameObject optionsObject = Instantiate(optionsPrefab, startCanvas.transform);
            OptionsMenu menuOptions = optionsObject.GetComponent<OptionsMenu>();
            menuOptions.SetGlobalCallbacks(this);
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        public PlayerInputType GetPlayerInput()
        {
            PlayerInputType input = PlayerInputType.DefaultNone;

            if (Input.GetKeyDown(interactUp))
            {
                input = PlayerInputType.NavigateUp;
            }
            else if (Input.GetKeyDown(interactLeft))
            {
                input = PlayerInputType.NavigateLeft;
            }
            else if (Input.GetKeyDown(interactRight))
            {
                input = PlayerInputType.NavigateRight;
            }
            else if (Input.GetKeyDown(interactDown))
            {
                input = PlayerInputType.NavigateDown;
            }
            else if (Input.GetKeyDown(interactKeyOne))
            {
                input = PlayerInputType.Execute;
            }
            else if (Input.GetButtonDown(interactCancelButton))
            {
                input = PlayerInputType.Cancel;
            }

            return input;
        }
    }
}