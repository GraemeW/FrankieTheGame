using UnityEngine;
using Frankie.Saving;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    public class Launcher : UIBox<UIBoxState>
    {
        // Tunables
        [Header("Start Menu-Specific")]
        [SerializeField] private OptionsMenu optionsPrefab;
        [SerializeField] private LoadGameMenu loadGamePrefab;
        
        // Cached References
        private Canvas startCanvas;

        protected override void AwakeTriggered()
        {
            clearVolatileOptionsOnEnable = false;
            preventEscapeOptionExit = true;
        }

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
