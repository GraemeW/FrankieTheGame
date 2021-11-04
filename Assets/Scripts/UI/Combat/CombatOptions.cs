using Frankie.Stats;
using Frankie.Utils.UI;
using Frankie.Speech.UI;
using Frankie.Stats.UI;
using Frankie.Inventory.UI;
using UnityEngine;
using Frankie.Control;
using System;

namespace Frankie.Combat.UI
{
    public class CombatOptions : UIBox
    {
        // Tunables 
        [SerializeField] GameObject statusPrefab = null;
        [SerializeField] GameObject knapsackPrefab = null;

        // Cached References
        BattleController battleController = null;
        BattleCanvas battleCanvas = null;
        Party party = null;

        public void Setup(BattleController battleController, BattleCanvas battleCanvas, Party party)
        {
            this.battleController = battleController;
            this.battleCanvas = battleCanvas;
            this.party = party;
        }

        public void InitiateCombat() // Called via unity events
        {
            battleController.SetBattleState(BattleState.Combat);
            gameObject.SetActive(false);
        }

        public void OpenStats() // Called via unity events
        {
            GameObject childOption = Instantiate(statusPrefab, battleCanvas.transform);
            StatusBox statusBox = childOption.GetComponent<StatusBox>();
            statusBox.Setup(party);
            PassControl(this, new Action[] { () => EnableInput(true) }, statusBox, battleController);
        }

        public void OpenKnapsack() // Called via unity events
        {
            GameObject childOption = Instantiate(knapsackPrefab, battleCanvas.transform);
            InventoryBox inventoryBox = childOption.GetComponent<InventoryBox>();
            inventoryBox.Setup(battleController, party);
            PassControl(this, new Action[] { () => SetCombatOptions(true) }, inventoryBox, battleController);
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
                battleCanvas.SetupRunFailureMessage(this, new Action[] { () => SetCombatOptions(false) });
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

        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return true; } // Spoof:  Cannot accept input, so treat as if global input already handled

            if (!IsChoiceAvailable()) { return false; } // Childed objects can still accept input on no choices available
            if (ShowCursorOnAnyInteraction(playerInputType)) { return true; }
            if (PrepareChooseAction(playerInputType)) { return true; }
            if (MoveCursor(playerInputType)) { return true; }

            return false;
        }
    }
}