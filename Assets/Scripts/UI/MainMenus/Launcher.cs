using UnityEngine;
using Frankie.Saving;
using Frankie.ZoneManagement;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    public class Launcher : UIBox
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
            SetActiveInput(false);
            SavingWrapper.LoadStartScene();
        }

        public void LoadGame() // Called via Unity Events
        {
            LoadGameMenu loadGameMenu = Instantiate(loadGamePrefab, startCanvas.transform);
            loadGameMenu.Setup(newGameZoneOverride);
            
            SetActiveInput(false);
            controller.AddInputReceiver(loadGameMenu, null);
        }

        public void Continue() // Called via Unity Events -- Standard Continue
        {
            SetActiveInput(false);
            SavingWrapper.Continue();
        }

        public void SaveCorePlayerStateAndContinue() // Called via Unity Events -- GameOver Continue
        {
            SetActiveInput(false);
            SavingWrapper.SaveCorePlayerStateToSave();
            SavingWrapper.Continue();
        }

        public void LoadOptions() // Called via Unity Events
        {
            OptionsMenu menuOptions = Instantiate(optionsPrefab, startCanvas.transform);
            controller.AddInputReceiver(menuOptions, null);
        }

        public void ExitGame() // Called via Unity Events
        {
            SetActiveInput(false);
            Application.Quit();
        }
    }
}
