using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;
using Frankie.Utils;
using Frankie.Utils.Localization;

namespace Frankie.Control
{
    public abstract class CheckConfiguration : ScriptableObject, ILocalizable
    {
        public abstract string GetMessage();
        public abstract List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateHandler, CheckWithConfiguration callingCheck);
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.ChecksWorldObjects;
        public virtual List<TableEntryReference> GetLocalizationEntries() => new();
        
        protected static void AddDialogueSpawnOptionForConfiguration(ref List<ChoiceActionPair> interactActions, PlayerStateMachine playerStateHandler, 
            CheckWithConfiguration callingCheck, string choiceOption, CheckConfiguration checkConfiguration)
        {
            string message = checkConfiguration.GetMessage();
            List<ChoiceActionPair> subInteractActions = checkConfiguration.GetChoiceActionPairs(playerStateHandler, callingCheck);

            if (subInteractActions is not { Count: > 0 }) return;
            var choiceActionPair = new ChoiceActionPair(choiceOption,
                () => playerStateHandler.EnterDialogue(message, subInteractActions));
            interactActions.Add(choiceActionPair);
        }
    }
}
