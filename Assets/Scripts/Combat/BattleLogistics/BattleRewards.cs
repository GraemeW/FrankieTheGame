using System;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Inventory;
using Frankie.Stats;

namespace Frankie.Combat
{
    public class BattleRewards : MonoBehaviour
    {
        // State
        private float battleExperienceReward;
        private readonly List<Tuple<string, InventoryItem>> allocatedLootCart = new();
        private readonly List<Tuple<string, InventoryItem>> unallocatedLootCart = new();

        #region PublicMethods
        public float GetBattleExperienceReward() => battleExperienceReward;
        public List<Tuple<string, InventoryItem>> GetAllocatedLootCart() => allocatedLootCart;
        public List<Tuple<string, InventoryItem>> GetUnallocatedLootCart() => unallocatedLootCart;
        public bool HasLootCart() => HasAllocatedLootCart() || HasUnallocatedLootCart();
        public bool HasAllocatedLootCart() => allocatedLootCart is { Count: > 0 };
        public bool HasUnallocatedLootCart() => unallocatedLootCart is { Count: > 0 };

        public bool HandleBattleRewardsTriggered(PartyCombatConduit partyCombatConduit, IList<BattleEntity> activePlayerCharacters, IList<BattleEntity> enemies)
        {
            bool isLevelUpPending = AwardExperienceToLevelUp(activePlayerCharacters, enemies);
            bool isLootPending = AwardLoot(partyCombatConduit, enemies);

            return isLevelUpPending || isLootPending;
        }
        #endregion

        #region PrivateMethods
        private bool AwardExperienceToLevelUp(IList<BattleEntity> activePlayerCharacters, IList<BattleEntity> enemies)
        {
            bool levelUpTriggered = false;
            foreach (BattleEntity character in activePlayerCharacters)
            {
                var experience = character.combatParticipant.GetComponent<Experience>();
                if (experience == null) { continue; } // Handling if characters do not level
                float scaledExperienceReward = 0f;

                foreach (BattleEntity enemy in enemies)
                {
                    float rawExperienceReward = enemy.combatParticipant.GetExperienceReward();

                    int levelDelta = character.combatParticipant.GetLevel() - enemy.combatParticipant.GetLevel();
                    scaledExperienceReward += Experience.GetScaledExperience(rawExperienceReward, levelDelta);
                    battleExperienceReward += scaledExperienceReward;
                }

                scaledExperienceReward = Mathf.Min(scaledExperienceReward, Experience.GetMaxExperienceReward());
                levelUpTriggered = experience.GainExperienceToLevel(scaledExperienceReward);
            }

            return levelUpTriggered;
        }

        private bool AwardLoot(PartyCombatConduit partyCombatConduit, IEnumerable<BattleEntity> enemies)
        {
            var partyKnapsackConduit = partyCombatConduit.GetComponent<PartyKnapsackConduit>();
            var wallet = partyCombatConduit.GetComponent<Wallet>();
            if (partyKnapsackConduit == null || wallet == null) { return false; } // Failsafe, this should not happen

            bool lootAvailable = false;
            foreach (BattleEntity enemy in enemies)
            {
                if (!enemy.combatParticipant.HasLoot()) { continue; }
                if (!enemy.combatParticipant.TryGetComponent(out LootDispenser lootDispenser)) { continue; }
                lootAvailable = true;

                foreach (InventoryItem inventoryItem in lootDispenser.GetItemReward())
                {
                    var enemyItemPair = new Tuple<string, InventoryItem>(enemy.combatParticipant.GetCombatName(), inventoryItem);
                    if (partyKnapsackConduit.AddToFirstEmptyPartySlot(inventoryItem))
                    {
                        allocatedLootCart.Add(enemyItemPair);
                    }
                    else
                    {
                        unallocatedLootCart.Add(enemyItemPair);
                    }
                }

                wallet.UpdatePendingCash(lootDispenser.GetCashReward());
            }
            return lootAvailable;
        }
        #endregion
    }
}
