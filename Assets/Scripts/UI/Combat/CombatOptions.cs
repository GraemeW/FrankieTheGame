using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Stats;
using Frankie.Utils;
using Frankie.Stats.UI;
using Frankie.Inventory.UI;
using Frankie.Utils.UI;
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

        // UIBox Configuration
        protected override EnumLookupBase<UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var combatOptionsConfiguration = new EnumLookup<UIBoxState,UIBoxStateBehaviour>();
            var defaultStateBehaviour = new UIBoxStateBehaviour( 
                moveCursor: (controllerInputType, _) => MoveCursor2D(controllerInputType) 
                );
            combatOptionsConfiguration.TrySet(UIBoxState.Default, defaultStateBehaviour);
            return combatOptionsConfiguration;
        }
        
        #region UnityMethods

        protected override void AwakeTriggered()
        {
            preventEscapeOptionExit = true;
        }

        protected override void StartTriggered()
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
            battleController.AddInputReceiver(statusBox, EnableCombatOptions);
            gameObject.SetActive(false);
        }

        public void OpenKnapsack() // Called via unity events
        {
            InventoryBox inventoryBox = Instantiate(inventoryBoxPrefab, battleCanvas.transform);
            inventoryBox.Setup(battleController, partyCombatConduit, null);
            battleController.AddInputReceiver(inventoryBox, EnableCombatOptions);
            gameObject.SetActive(false);
        }

        public void AttemptToRun() // Called via unity events
        {
            SetActiveInput(false);
            if (battleController.AttemptToRun())
            {
                gameObject.SetActive(false);
            }
            else
            {
                battleCanvas.SetupRunFailureMessage(InitiateCombat);
                gameObject.SetActive(false);
            }
        }

        public void EnableCombatOptions()
        {
            gameObject.SetActive(true);
            SetActiveInput(true);
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
    }
}
