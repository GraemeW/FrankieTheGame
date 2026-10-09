using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Inventory;
using Frankie.Inventory.UI;
using Frankie.Speech.UI;
using Frankie.Stats;
using Frankie.Utils.Localization;
using Object = UnityEngine.Object;

namespace Frankie.Combat.UI
{
    [Serializable]
    public sealed class BattleLootMessages
    {
        // Note:  Each step is handed an onComplete to call once the player is through it (see CombatMessages' sequence queue)

        // Tunables
        [Header("Prefabs")]
        [SerializeField] private DialogueOptionBox dialogueOptionBoxPrefab;
        [SerializeField] private InventorySwapBox inventorySwapBoxPrefab;
        [Header("Include {0} for leader character name")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageGainedLoot;
        [Header("Include {0} for enemy name, {1} for item name")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageEnemyDroppedLoot;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageEnemyDroppedLootNoRoom;
        [Header("Include {0} for item name")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageConfirmThrowOut;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionChuckItemAffirmative;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionChuckItemNegative;

        // Cached References
        private BattleController battleController;
        private BattleRewards battleRewards;
        private PartyCombatConduit partyCombatConduit;
        private DialogueBox dialogueBoxPrefab;
        private Transform messageParent;

        #region PublicMethods
        public void Setup(BattleController setBattleController, PartyCombatConduit setPartyCombatConduit, DialogueBox setDialogueBoxPrefab, Transform setMessageParent)
        {
            battleController = setBattleController;
            battleRewards = setBattleController.GetBattleRewards();
            partyCombatConduit = setPartyCombatConduit;
            dialogueBoxPrefab = setDialogueBoxPrefab;
            messageParent = setMessageParent;
        }

        public IEnumerable<TableEntryReference> GetLocalizationEntries()
        {
            yield return localizedMessageGainedLoot.TableEntryReference;
            yield return localizedMessageEnemyDroppedLoot.TableEntryReference;
            yield return localizedMessageEnemyDroppedLootNoRoom.TableEntryReference;
            yield return localizedMessageConfirmThrowOut.TableEntryReference;
            yield return localizedOptionChuckItemAffirmative.TableEntryReference;
            yield return localizedOptionChuckItemNegative.TableEntryReference;
        }

        // The loot steps for a won battle, in order (empty otherwise)
        public IEnumerable<Action<Action>> GetSequences(BattleOutcome battleOutcome)
        {
            if (battleOutcome != BattleOutcome.Won || battleRewards == null) { yield break; }

            yield return ShowFoundLoot;
            yield return ShowAllocatedLoot;
            foreach (Tuple<string, InventoryItem> enemyItemPair in battleRewards.GetUnallocatedLootCart())
            {
                string enemyName = enemyItemPair.Item1;
                InventoryItem inventoryItem = enemyItemPair.Item2;
                yield return onComplete => ShowUnallocatedLoot(enemyName, inventoryItem, onComplete);
            }
        }
        #endregion

        #region PrivateMethods
        private void ShowFoundLoot(Action onComplete)
        {
            if (!battleRewards.HasLootCart()) { onComplete(); return; }

            DialogueBox dialogueBox = Object.Instantiate(dialogueBoxPrefab, messageParent);
            dialogueBox.AddText(string.Format(localizedMessageGainedLoot.GetSafeLocalizedString(), partyCombatConduit.GetPartyLeaderName()));
            battleController.AddInputReceiver(dialogueBox, onComplete);
        }

        // Items that were placed straight into a knapsack
        private void ShowAllocatedLoot(Action onComplete)
        {
            if (!battleRewards.HasAllocatedLootCart()) { onComplete(); return; }

            DialogueBox dialogueBox = Object.Instantiate(dialogueBoxPrefab, messageParent);
            foreach (Tuple<string, InventoryItem> enemyItemPair in battleRewards.GetAllocatedLootCart())
            {
                dialogueBox.AddText(string.Format(localizedMessageEnemyDroppedLoot.GetSafeLocalizedString(), enemyItemPair.Item1, enemyItemPair.Item2.GetDisplayName()));
                dialogueBox.AddPageBreak();
            }
            battleController.AddInputReceiver(dialogueBox, onComplete);
        }

        // An item that didn't fit (knapsacks full):  swap something out for it, or confirm throwing it away
        // Note:  Backing out of any box in this chain re-spawns this prompt (avoid lost loot) - only a swap or a confirmed throw-out completes the step
        private void ShowUnallocatedLoot(string enemyName, InventoryItem inventoryItem, Action onComplete)
        {
            if (!battleRewards.HasUnallocatedLootCart()) { onComplete(); return; }

            DialogueOptionBox dialogueOptionBox = Object.Instantiate(dialogueOptionBoxPrefab, messageParent);
            dialogueOptionBox.Setup(string.Format(localizedMessageEnemyDroppedLootNoRoom.GetSafeLocalizedString(), enemyName, inventoryItem.GetDisplayName()));

            var choiceActionPairs = new List<ChoiceActionPair>
            {
                new(localizedOptionChuckItemAffirmative.GetSafeLocalizedString(), () =>
                {
                    ShowInventorySwapBox(enemyName, inventoryItem, onComplete);
                    Object.Destroy(dialogueOptionBox.gameObject);
                }),
                new(localizedOptionChuckItemNegative.GetSafeLocalizedString(), () =>
                {
                    ShowConfirmThrowOut(enemyName, inventoryItem, onComplete);
                    Object.Destroy(dialogueOptionBox.gameObject);
                })
            };
            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);

            battleController.AddInputReceiver(dialogueOptionBox, () => ShowUnallocatedLoot(enemyName, inventoryItem, onComplete));
            dialogueOptionBox.ClearDisableCallbacksOnChoose(true);
        }

        private void ShowInventorySwapBox(string enemyName, InventoryItem inventoryItem, Action onComplete)
        {
            InventorySwapBox inventorySwapBox = Object.Instantiate(inventorySwapBoxPrefab, messageParent);

            // Note:  The swap box destroys itself on a successful swap
            inventorySwapBox.Setup(battleController, partyCombatConduit, inventoryItem, () =>
            {
                inventorySwapBox.ClearDisableCallbacks();
                onComplete();
            });
            battleController.AddInputReceiver(inventorySwapBox, () => ShowUnallocatedLoot(enemyName, inventoryItem, onComplete));
        }

        private void ShowConfirmThrowOut(string enemyName, InventoryItem inventoryItem, Action onComplete)
        {
            DialogueOptionBox dialogueOptionBox = Object.Instantiate(dialogueOptionBoxPrefab, messageParent);
            dialogueOptionBox.Setup(string.Format(localizedMessageConfirmThrowOut.GetSafeLocalizedString(), inventoryItem.GetDisplayName()));

            var choiceActionPairs = new List<ChoiceActionPair>
            {
                new(localizedOptionChuckItemAffirmative.GetSafeLocalizedString(), () =>
                {
                    dialogueOptionBox.ClearDisableCallbacks();
                    onComplete();
                }),
                new(localizedOptionChuckItemNegative.GetSafeLocalizedString(), () => { }) // Otherwise loop back & re-spawn (via the disable callback)
            };
            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);
            battleController.AddInputReceiver(dialogueOptionBox, () => ShowUnallocatedLoot(enemyName, inventoryItem, onComplete));
        }
        #endregion
    }
}
