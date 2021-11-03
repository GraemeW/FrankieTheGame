using Frankie.Control;
using Frankie.Core;
using Frankie.Utils.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Menu.UI
{
    public class StartMenu : UIBox
    {
        // Tunables
        [Header("Start Menu-Specific")]
        [SerializeField] OptionsMenu optionsPrefab = null;
        [SerializeField] LoadGameMenu loadGamePrefab = null;

        // Cached References
        SavingWrapper savingWrapper = null;
        Canvas startCanvas = null;

        private void Start()
        {
            // SavingWrapper is a persistent object, thus can only be found after Awake -- so find in Start
            savingWrapper = GameObject.FindGameObjectWithTag("Saver").GetComponent<SavingWrapper>();
        }

        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, Canvas startCanvas)
        {
            this.startCanvas = startCanvas;
            SetGlobalInputHandler(standardPlayerInputCaller);

            // Toggle to set up global input handling
            gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        public void LoadGame() // Called via Unity Events
        {
            LoadGameMenu loadGameMenu = Instantiate(loadGamePrefab, startCanvas.transform);
            EnableInput(false);
            loadGameMenu.SetGlobalInputHandler(controller);
            loadGameMenu.SetDisableCallback(this, () => EnableInput(true));
        }

        public void Continue() // Called via Unity Events
        {
            savingWrapper.Continue();
        }

        public void LoadOptions() // Called via Unity Events
        {
            OptionsMenu menuOptions = Instantiate(optionsPrefab, startCanvas.transform);
            EnableInput(false);
            menuOptions.SetGlobalInputHandler(controller);
            menuOptions.SetDisableCallback(this, () => EnableInput(true));
        }

        public void ExitGame() // Called via Unity Events
        {
            Application.Quit();
        }

        
    }
}