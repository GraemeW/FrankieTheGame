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
        Canvas startCanvas = null;

        public void Setup(Canvas startCanvas)
        {
            this.startCanvas = startCanvas;

            // Toggle to set up global input handling
            gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        public void ReloadStartScreen() // Called via Unity Events
        {
            SavingWrapper.LoadStartScene();
        }

        public void LoadGame() // Called via Unity Events
        {
            LoadGameMenu loadGameMenu = Instantiate(loadGamePrefab, startCanvas.transform);
            EnableInput(false);
            PassControl(loadGameMenu);
        }

        public void Continue() // Called via Unity Events
        {
            SavingWrapper.Continue();
        }

        public void LoadOptions() // Called via Unity Events
        {
            OptionsMenu menuOptions = Instantiate(optionsPrefab, startCanvas.transform);
            PassControl(menuOptions);
        }

        public void ExitGame() // Called via Unity Events
        {
            Application.Quit();
        }
    }
}
