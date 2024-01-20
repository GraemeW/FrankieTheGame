using Frankie.Inventory;
using Frankie.Stats;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public class BattleRewards : MonoBehaviour
    {
        // State
        float battleExperienceReward = 0f;
        List<Tuple<string, InventoryItem>> allocatedLootCart = new List<Tuple<string, InventoryItem>>();
        List<Tuple<string, InventoryItem>> unallocatedLootCart = new List<Tuple<string, InventoryItem>>();

        #region PublicMethods
        public float GetBattleExperienceReward() => battleExperienceReward;
        public List<Tuple<string, InventoryItem>> GetAllocatedLootCart() => allocatedLootCart;
        public List<Tuple<string, InventoryItem>> GetUnallocatedLootCart() => unallocatedLootCart;
        public bool HasLootCart() => HasAllocatedLootCart() || HasUnallocatedLootCart();
        public bool HasAllocatedLootCart() => allocatedLootCart != null && allocatedLootCart.Count > 0;
        public bool HasUnallocatedLootCart() => unallocatedLootCart != null && unallocatedLootCart.Count > 0;

        public bool HandleBattleRewardsTriggered(PartyCombatConduit partyCombatConduit, IEnumerable<BattleEntity> activePlayerCharacters, IEnumerable<BattleEntity> enemies)
        {
            bool isLevelUpPending = AwardExperienceToLevelUp(activePlayerCharacters, enemies);
            bool isLootPending = AwardLoot(partyCombatConduit, enemies);

            return isLevelUpPending || isLootPending;
        }
        #endregion

        #region PrivateMethods
        private bool AwardExperienceToLevelUp(IEnumerable<BattleEntity> activePlayerCharacters, IEnumerable<BattleEntity> enemies)
        {
            bool levelUpTriggered = false;
            foreach (BattleEntity character in activePlayerCharacters)
            {
                Experience experience = character.combatParticipant.GetComponent<Experience>();
                if (experience == null) { continue; } // Handling for characters who do not level
                float scaledExperienceReward = 0f;

                foreach (BattleEntity enemy in enemies)
                {
                    float rawExperienceReward = enemy.combatParticipant.GetExperienceReward();

                    int levelDelta = character.combatParticipant.GetLevel() - enemy.combatParticipant.GetLevel();
                    scaledExperienceReward += Experience.GetScaledExperience(rawExperienceReward, levelDelta);
                    battleExperienceReward += scaledExperienceReward;
                }

                scaledExperienceReward = Mathf.Min(scaledExperienceReward, Experience.GetMaxExperienceReward());
                if (experience.GainExperienceToLevel(scaledExperienceReward))
                {
                    levelUpTriggered = true;
                }
            }

            return levelUpTriggered;
        }

        private bool AwardLoot(PartyCombatConduit partyCombatConduit, IEnumerable<BattleEntity> enemies)
        {
            PartyKnapsackConduit partyKnapsackConduit = partyCombatConduit.GetComponent<PartyKnapsackConduit>();
            Wallet wallet = partyCombatConduit.GetComponent<Wallet>();
            if (partyKnapsackConduit == null || wallet == null) { return false; } // Failsafe, this should not happen

            bool lootAvailable = false;
            foreach (BattleEntity enemy in enemies)
            {
                if (!enemy.combatParticipant.HasLoot()) { continue; }
                if (!enemy.combatParticipant.TryGetComponent(out LootDispenser lootDispenser)) { continue; }
                lootAvailable = true;

                foreach (InventoryItem inventoryItem in lootDispenser.GetItemReward())
                {
                    CombatParticipant receivingCharacter = partyKnapsackConduit.AddToFirstEmptyPartySlot(inventoryItem);
                    Tuple<string, InventoryItem> enemyItemPair = new Tuple<string, InventoryItem>(enemy.combatParticipant.GetCombatName(), inventoryItem);
                    if (receivingCharacter != null)
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
