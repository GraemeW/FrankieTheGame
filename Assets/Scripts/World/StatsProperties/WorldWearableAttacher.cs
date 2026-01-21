using UnityEngine;
using Frankie.Control;
using Frankie.Inventory;
using Frankie.Stats;

namespace Frankie.World
{
    public class WorldWearableAttacher : MonoBehaviour
    {
        // Tunables
        [SerializeField] private Wearable wearable;


        // Public Methods
        public void SpawnWearableOnLead(PlayerStateMachine playerStateMachine) // Called via Unity events
        {
            if (wearable == null) { return; }

            BaseStats partyLead = playerStateMachine.GetParty().GetPartyLeader();
            if (partyLead.TryGetComponent(out WearablesLink wearablesLink))
            {
                SpawnWearable(wearablesLink);
            }
        }

        public void SpawnWearableOnParty(PlayerStateMachine playerStateMachine) // Called via Unity events
        {
            if (wearable == null) { return; }

            foreach (BaseStats character in playerStateMachine.GetParty().GetParty())
            {
                if (character.TryGetComponent(out WearablesLink wearablesLink))
                {
                    SpawnWearable(wearablesLink);
                }
            }
        }

        private void SpawnWearable(WearablesLink wearablesLink)
        {
            WearableItem wearableItem = wearable.GetWearableItem();
            if (wearableItem != null && wearableItem.IsUnique())
            {
                if (wearablesLink.IsWearingItem(wearableItem)) { return; }
            }

            Wearable spawnedWearable = Instantiate(wearable, wearablesLink.GetAttachedObjectsRoot());
            spawnedWearable.AttachToCharacter(wearablesLink);
        }
    }
}
