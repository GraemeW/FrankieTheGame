using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;

namespace Frankie.Control
{
    public abstract class CheckConfiguration : ScriptableObject
    {
        public abstract string GetMessage();
        public abstract List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateHandler, CheckWithConfiguration callingCheck);

        public void AddDialogueSpawnOptionForConfiguration(ref List<ChoiceActionPair> interactActions, PlayerStateMachine playerStateHandler, CheckWithConfiguration callingCheck,
            string choiceOption, CheckConfiguration checkConfiguration)
        {
            string message = checkConfiguration.GetMessage();
            List<ChoiceActionPair> subInteractActions = checkConfiguration.GetChoiceActionPairs(playerStateHandler, callingCheck);

            if (subInteractActions != null && subInteractActions.Count > 0)
            {
                ChoiceActionPair choiceActionPair = new ChoiceActionPair(choiceOption,
                    () => playerStateHandler.EnterDialogue(message, subInteractActions));
                interactActions.Add(choiceActionPair);
            }
        }
    }
}
