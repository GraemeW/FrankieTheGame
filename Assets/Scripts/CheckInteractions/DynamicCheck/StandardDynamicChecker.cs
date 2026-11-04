using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Core;
using Frankie.Utils.Localization;
using UnityEngine.Localization.Tables;


namespace Frankie.Control
{
    [ExecuteInEditMode]
    public class StandardDynamicChecker : MonoBehaviour, ICheckDynamic, ILocalizable
    {
        // Tunables
        [Tooltip("Note - {0} for party leader, {1} for check object name")][SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString dynamicCheckMessage;
        [SerializeField] private List<DynamicCheckOption> interactionOptions = new();
        
        #region Localization
        public LocalizationTableType localizationTableType { get; } =  LocalizationTableType.ChecksWorldObjects;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            var localizationEntries = new List<TableEntryReference> { dynamicCheckMessage.TableEntryReference };
            foreach (DynamicCheckOption interactionOption in interactionOptions)
            {
                if (interactionOption?.GetLocalizedChoice() == null) { continue; }
                localizationEntries.Add(interactionOption.GetLocalizedChoice().TableEntryReference);
            }
            return localizationEntries;
        }
        #endregion
        
        #region UnityMethods
        protected void OnDestroy()
        {
            ILocalizable.TriggerOnDestroy(this);
        }
        #endregion
        
        #region PublicMethods

        public string GetMessage(PlayerStateMachine playerStateMachine)
        {
            string partyLeaderName = playerStateMachine.GetParty().GetPartyLeaderName();
            if (string.IsNullOrWhiteSpace(partyLeaderName)) { partyLeaderName = CheckBase.defaultPartyLeaderName; }
            
            return string.Format(dynamicCheckMessage.GetSafeLocalizedString(), partyLeaderName, CheckBase.GetCheckObjectName(transform));
        }

        public List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateMachine)
        {
            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            foreach (DynamicCheckOption interactionOption in interactionOptions)
            {
                LocalizedString localizedChoice = interactionOption?.GetLocalizedChoice();
                if (localizedChoice == null || localizedChoice.IsEmpty) { continue; }
                
                string choice = localizedChoice.GetSafeLocalizedString();
                InteractionEvent interactionEvent = interactionOption.GetInteractionEvent();
                if (string.IsNullOrEmpty(choice) || interactionEvent == null) { continue; }
                
                choiceActionPairs.Add(new ChoiceActionPair(choice, () => interactionEvent.Invoke(playerStateMachine)));
            }
            return choiceActionPairs;
        }
        #endregion
    }
}
