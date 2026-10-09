using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.UIBox;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Stats;
using Frankie.Stats.UI;
using Frankie.Inventory.UI;
using Frankie.Utils.Localization;

namespace Frankie.Combat.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class CombatOptions : UIBox<UIBoxState>, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedFightText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedItemText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedStatsText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedRunawayText;
        [Header("Prefabs")]
        [SerializeField] private StatusBox statusBoxPrefab;
        [SerializeField] private InventoryBox inventoryBoxPrefab;
        
        // Cached References
        private BattleController battleController;
        private CombatMessages combatMessages;
        private Transform boxParent;
        private PartyCombatConduit partyCombatConduit;

        // UIBox Configuration
        protected override EnumLookup<UIBoxState,UIBoxStateBehaviour> BuildStateBehaviours()
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
            clearVolatileOptionsOnEnable = false;

            // Note:  Choice order is row-by-row over two columns (MoveCursor2D)
            AddNonDestroyChoiceOption(localizedFightText.GetSafeLocalizedString(), InitiateCombat);
            AddNonDestroyChoiceOption(localizedItemText.GetSafeLocalizedString(), OpenKnapsack);
            AddNonDestroyChoiceOption(localizedStatsText.GetSafeLocalizedString(), OpenStats);
            AddNonDestroyChoiceOption(localizedRunawayText.GetSafeLocalizedString(), AttemptToRun);
        }
        #endregion
        
        #region PubicMethods
        public void Setup(BattleController setBattleController, PartyCombatConduit setPartyCombatConduit, CombatMessages setCombatMessages, Transform setBoxParent)
        {
            battleController = setBattleController;
            partyCombatConduit = setPartyCombatConduit;
            combatMessages = setCombatMessages;
            boxParent = setBoxParent;
        }

        public void EnableCombatOptions()
        {
            gameObject.SetActive(true);
            SetActiveInput(true);
        }
        #endregion

        #region PrivateMethods
        private void InitiateCombat()
        {
            battleController.SetBattleState(BattleState.Combat, BattleOutcome.Undetermined);
            gameObject.SetActive(false);
        }

        private void OpenStats()
        {
            StatusBox statusBox = Instantiate(statusBoxPrefab, boxParent);
            statusBox.Setup(partyCombatConduit);
            battleController.AddInputReceiver(statusBox, EnableCombatOptions);
            gameObject.SetActive(false);
        }

        private void OpenKnapsack()
        {
            InventoryBox inventoryBox = Instantiate(inventoryBoxPrefab, boxParent);
            inventoryBox.Setup(battleController, partyCombatConduit, null);
            battleController.AddInputReceiver(inventoryBox, EnableCombatOptions);
            gameObject.SetActive(false);
        }

        private void AttemptToRun()
        {
            SetActiveInput(false);
            if (!battleController.AttemptToRun()) { combatMessages.ShowRunFailureMessage(InitiateCombat); }
            gameObject.SetActive(false);
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
