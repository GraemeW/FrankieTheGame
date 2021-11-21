using Frankie.Combat;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    public class PartyKnapsackConduit : MonoBehaviour
    {
        // State
        List<Knapsack> knapsacks = new List<Knapsack>();

        // Cached References
        Party party = null;

        #region UnityMethods
        private void Awake()
        {
            party = GetComponent<Party>();
        }

        private void Start()
        {
            RefreshKnapsacks();
        }

        private void OnEnable()
        {
            party.partyUpdated += RefreshKnapsacks;
        }

        private void OnDisable()
        {
            party.partyUpdated -= RefreshKnapsacks;
        }

        private void RefreshKnapsacks()
        {
            knapsacks.Clear();
            foreach (CombatParticipant character in party.GetParty())
            {
                Knapsack knapsack = character.GetKnapsack();
                knapsacks.Add(knapsack);
            }
        }
        #endregion

        #region PublicMethods
        public CombatParticipant AddToFirstEmptyPartySlot(InventoryItem inventoryItem)
        {
            foreach (Knapsack knapsack in knapsacks)
            {
                if (knapsack.AddToFirstEmptySlot(inventoryItem, true))
                {
                    return knapsack.GetCharacter();
                }
            }
            return null;
        }

        public int GetNumberOfFreeSlotsInParty()
        {
            int freeSlots = 0;
            foreach (Knapsack knapsack in knapsacks)
            {
                freeSlots += knapsack.GetNumberOfFreeSlots();
            }
            return freeSlots;
        }

        #endregion
    }
}
