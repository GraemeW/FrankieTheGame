using Frankie.Combat;
using Frankie.Inventory;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Control.Specialization
{
    public class WorldWearableAttacher : MonoBehaviour
    {
        // Tunables
        [SerializeField] Wearable wearable = null;


        // Public Methods
        public void SpawnWearableOnLead(PlayerStateMachine playerStateMachine) // Called via Unity events
        {
            if (wearable == null) { return; }

            BaseStats partyLead = playerStateMachine.GetParty().GetPartyLeader();
            if (partyLead.TryGetComponent(out CharacterSpriteLink characterSpriteLink))
            {
                Wearable spawnedWearable = Instantiate(wearable, characterSpriteLink.GetAttachedObjectsRoot());
                spawnedWearable.AttachToCharacter(characterSpriteLink);
            }
        }

        public void SpawnWearableOnParty(PlayerStateMachine playerStateMachine) // Called via Unity events
        {
            if (wearable == null) { return; }

            foreach (BaseStats character in playerStateMachine.GetParty().GetParty())
            {
                if (character.TryGetComponent(out CharacterSpriteLink characterSpriteLink))
                {
                    Wearable spawnedWearable = Instantiate(wearable);
                    spawnedWearable.AttachToCharacter(characterSpriteLink);
                }
            }
        }
    }
}
