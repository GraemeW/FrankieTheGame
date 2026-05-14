using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Control;
using Frankie.Stats;
using Frankie.Utils.UI;
using Frankie.Stats.UI;
using Frankie.Inventory.UI;
using Frankie.Speech.UI;
using Frankie.Utils.Localization;

namespace Frankie.Combat.UI
{
    public class CombatOptions : UIBox, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedFightText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedItemText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedStatsText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedRunawayText;
        [Header("Hookups")]
        [SerializeField] private UIChoice fightChoiceOption;
        [SerializeField] private UIChoice itemChoiceOption;
        [SerializeField] private UIChoice statsChoiceOption;
        [SerializeField] private UIChoice runawayChoiceOption;
        [Header("Prefabs")]
        [SerializeField] private StatusBox statusBoxPrefab;
        [SerializeField] private InventoryBox inventoryBoxPrefab;

        // Cached References
        private BattleController battleController;
        private BattleCanvas battleCanvas;
        private PartyCombatConduit partyCombatConduit;

        #region UnityMethods
        private void Start()
        {
            if (fightChoiceOption != null) { fightChoiceOption.SetText(localizedFightText.GetSafeLocalizedString()); }
            if (itemChoiceOption != null) { itemChoiceOption.SetText(localizedItemText.GetSafeLocalizedString()); }
            if (statsChoiceOption != null) { statsChoiceOption.SetText(localizedStatsText.GetSafeLocalizedString()); }
            if (runawayChoiceOption != null) { runawayChoiceOption.SetText(localizedRunawayText.GetSafeLocalizedString()); }
        }
        #endregion
        
        #region PubicMethods
        public void Setup(BattleController setBattleController, BattleCanvas setBattleCanvas, PartyCombatConduit setPartyCombatConduit)
        {
            battleController = setBattleController;
            battleCanvas = setBattleCanvas;
            partyCombatConduit = setPartyCombatConduit;
        }

        public void InitiateCombat() // Called via unity events
        {
            battleController.SetBattleState(BattleState.Combat, BattleOutcome.Undetermined);
            gameObject.SetActive(false);
        }

        public void OpenStats() // Called via unity events
        {
            StatusBox statusBox = Instantiate(statusBoxPrefab, battleCanvas.transform);
            statusBox.Setup(partyCombatConduit);
            PassControl(this, new Action[] { EnableCombatOptions }, statusBox, battleController);
        }

        public void OpenKnapsack() // Called via unity events
        {
            InventoryBox inventoryBox = Instantiate(inventoryBoxPrefab, battleCanvas.transform);
            inventoryBox.Setup(battleController, partyCombatConduit);
            PassControl(this, new Action[] { EnableCombatOptions }, inventoryBox, battleController);
            gameObject.SetActive(false);
        }

        public void AttemptToRun() // Called via unity events
        {
            EnableInput(false);
            if (battleController.AttemptToRun())
            {
                gameObject.SetActive(false);
            }
            else
            {
                DialogueBox runFailureDialogue = battleCanvas.SetupRunFailureMessage(this);
                PassControl(this, new Action[] { InitiateCombat }, runFailureDialogue, battleController);
                gameObject.SetActive(false);
            }
        }

        public void EnableCombatOptions()
        {
            gameObject.SetActive(true);
            EnableInput(true);
        }
        #endregion

        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedFightText.TableEntryReference,
                localizedItemText.TableEntryReference,
                localizedStatsText.TableEntryReference,
                localizedRunawayText.TableEntryReference
            };
        }
        #endregion
        
        #region InterfaceMethods
        protected override bool MoveCursor(PlayerInputType playerInputType)
        {
            return MoveCursor2D(playerInputType);
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
        #endregion
    }
}
