using UnityEngine;
using Frankie.Core;
using Frankie.Utils.UI;
using Frankie.ZoneManagement;

namespace Frankie.Menu.UI
{
    public class StartMenu : UIBox
    {
        // Tunables
        [Header("Start Menu-Specific")]
        [SerializeField] private OptionsMenu optionsPrefab;
        [SerializeField] private LoadGameMenu loadGamePrefab;
        [SerializeField] [Tooltip("Leave as blank to use default")] private Zone newGameZoneOverride;
        
        // Cached References
        private Canvas startCanvas;

        public void Setup(Canvas setStartCanvas)
        {
            startCanvas = setStartCanvas;

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
            loadGameMenu.Setup(newGameZoneOverride);
            EnableInput(false);
            PassControl(loadGameMenu);
        }

        public void Continue() // Called via Unity Events -- Standard Continue
        {
            SavingWrapper.Continue();
        }

        public void SaveCorePlayerStateAndContinue() // Called via Unity Events -- GameOver Continue
        {
            SavingWrapper.SaveCorePlayerStateToSave();
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
