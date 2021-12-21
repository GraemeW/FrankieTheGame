using Frankie.Combat;
using Frankie.Stats;
using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Frankie.Control
{
    public class CheckWithPartySelection : CheckBase
    {
        [SerializeField] string checkMessage = "Who do you want to select?";
        [SerializeField] InteractionEventWithCombatParticipant interactionEventWithCombatParticipant = null;

        public override bool HandleRaycast(PlayerStateHandler playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (string.IsNullOrEmpty(checkMessage)) { return false; }
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                List<ChoiceActionPair> interactActions = new List<ChoiceActionPair>();
                foreach (CombatParticipant character in playerStateHandler.GetParty().GetParty())
                {
                    interactActions.Add(new ChoiceActionPair(character.GetCombatName(), 
                        () => interactionEventWithCombatParticipant.Invoke(playerStateHandler, character)));
                }

                playerStateHandler.EnterDialogue(checkMessage, interactActions);
            }
            return true;
        }
    }
}