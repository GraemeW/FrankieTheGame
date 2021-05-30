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

        // Static
        protected static string DIALOGUE_CALLBACK_DISABLE_COMBAT_OPTIONS = "DISABLE_COMBAT_OPTIONS";
        protected static string DIALOGUE_CALLBACK_ENABLE_COMBAT_OPTIONS = "ENABLE_COMBAT_OPTIONS";

        protected override void Awake()
        {
            // Override default behavior, null implementation
        }

        protected override void Start()
        {
            // Override default behavior, null implementation
        }

        public void Setup(BattleController battleController, BattleCanvas battleCanvas, Party party)
        {
            this.battleController = battleController;
            this.battleCanvas = battleCanvas;
            this.party = party;

            SetGlobalCallbacks(battleController); // input handled via player controller, immediate override
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
            statusBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
        }

        public void OpenKnapsack() // Called via unity events
        {
            GameObject childOption = Instantiate(knapsackPrefab, battleCanvas.transform);
            InventoryBox inventoryBox = childOption.GetComponent<InventoryBox>();
            inventoryBox.Setup(battleController, party);
            inventoryBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_COMBAT_OPTIONS);
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
                runFailureDialogueBox.SetDisableCallback(this, DIALOGUE_CALLBACK_DISABLE_COMBAT_OPTIONS);
            }
        }

        public override void HandleDialogueCallback(DialogueBox dialogueBox, string callbackMessage)
        {
            base.HandleDialogueCallback(dialogueBox, callbackMessage);
            if (callbackMessage == DIALOGUE_CALLBACK_DISABLE_COMBAT_OPTIONS)
            {
                handleGlobalInput = true;
                InitiateCombat();
            }
            else if (callbackMessage == DIALOGUE_CALLBACK_ENABLE_COMBAT_OPTIONS)
            {
                handleGlobalInput = true;
                gameObject.SetActive(true);
            }
        }
    }
}