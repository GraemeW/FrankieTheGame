using Frankie.Speech.UI;
using Frankie.Stats.UI;
using Frankie.Stats;
using UnityEngine;
using UnityEngine.UI;
using Frankie.Inventory.UI;

namespace Frankie.Combat.UI
{
    public class CombatOptions : DialogueOptionBox
    {
        // Tunables 
        [SerializeField] GameObject statusPrefab = null;
        [SerializeField] GameObject knapsackPrefab = null;

        // Cached References
        BattleController battleController = null;
        BattleCanvas battleCanvas = null;
        Party party = null;

        protected override void Awake()
        {
            // Override default behavior, null implementation
        }

        public override void Setup(string optionText)
        {
            // Override default behavior, null implementation
        }

        public void Setup(BattleController battleController, BattleCanvas battleCanvas, Party party)
        {
            this.battleController = battleController;
            this.battleCanvas = battleCanvas;
            this.party = party;

            SetGlobalInputHandler(battleController); // input handled via player controller, immediate override
        }

        public void InitiateCombat() // Called via unity events
        {
            battleController.SetBattleState(BattleState.Combat);
            gameObject.SetActive(false);
        }

        public void OpenStats() // Called via unity events
        {
            handleGlobalInput = false;
            GameObject childOption = Instantiate(statusPrefab, battleCanvas.transform);
            StatusBox statusBox = childOption.GetComponent<StatusBox>();
            statusBox.Setup(battleController, party);
            statusBox.SetDisableCallback(this, () => EnableInput(true));
        }

        public void OpenKnapsack() // Called via unity events
        {
            GameObject childOption = Instantiate(knapsackPrefab, battleCanvas.transform);
            InventoryBox inventoryBox = childOption.GetComponent<InventoryBox>();
            inventoryBox.Setup(battleController, party);
            inventoryBox.SetDisableCallback(this, () => SetCombatOptions(true));
            gameObject.SetActive(false);
        }

        public void AttemptToRun() // Called via unity events
        {
            if (battleController.AttemptToRun())
            {
                gameObject.SetActive(false);
            }
            else
            {
                handleGlobalInput = false;
                DialogueBox runFailureDialogueBox = battleCanvas.SetupRunFailureMessage();
                runFailureDialogueBox.SetDisableCallback(this, () => SetCombatOptions(false));
            }
        }

        private void SetCombatOptions(bool enable)
        {
            if (!enable)
            {
                handleGlobalInput = true;
                InitiateCombat();
            }
            else
            {
                handleGlobalInput = true;
                gameObject.SetActive(true);
            }
        }
    }
}