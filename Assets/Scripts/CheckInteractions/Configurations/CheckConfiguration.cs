using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public abstract class CheckConfiguration : ScriptableObject
    {
        public abstract string GetMessage();
        public abstract List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateHandler playerStateHandler);

        public void AddDialogueSpawnOptionForConfiguration(ref List<ChoiceActionPair> interactActions, PlayerStateHandler playerStateHandler, 
            string choiceOption, CheckConfiguration checkConfiguration)
        {
            string message = checkConfiguration.GetMessage();
            List<ChoiceActionPair> subInteractActions = checkConfiguration.GetChoiceActionPairs(playerStateHandler);

            if (subInteractActions != null && subInteractActions.Count > 0)
            {
                ChoiceActionPair choiceActionPair = new ChoiceActionPair(choiceOption,
                    () => playerStateHandler.EnterDialogue(message, subInteractActions));
                interactActions.Add(choiceActionPair);
            }
        }
    }
}