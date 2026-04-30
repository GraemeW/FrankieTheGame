using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Utils;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Party Option Check Configuration", menuName = "CheckConfigurations/PartyOptions")]
    public class PartyOptionCheckConfiguration : CheckConfiguration
    {
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] protected LocalizedString localizedMessagePartyOptions;
        [SerializeField] private bool toggleLeaderAdjust = true;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] protected LocalizedString localizedOptionLeaderAdjust;
        [SerializeField] private CheckConfiguration partyLeaderConfiguration;
        [SerializeField] private bool toggleAddToParty = true;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] protected LocalizedString localizedOptionAddToParty;
        [SerializeField] private CheckConfiguration addToPartyConfiguration;
        [SerializeField] private bool toggleRemoveFromParty = true;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] protected LocalizedString localizedOptionRemoveFromParty;
        [SerializeField] private CheckConfiguration removeFromPartyConfiguration;

        public override string GetMessage() => localizedMessagePartyOptions.GetSafeLocalizedString();
        
        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateHandler, CheckWithConfiguration callingCheck)
        {
            var interactActions = new List<ChoiceActionPair>();
            if (toggleLeaderAdjust)
            {
                AddDialogueSpawnOptionForConfiguration(ref interactActions, playerStateHandler, callingCheck, localizedOptionLeaderAdjust.GetSafeLocalizedString(), partyLeaderConfiguration);
            }
            if (toggleAddToParty)
            {
                AddDialogueSpawnOptionForConfiguration(ref interactActions, playerStateHandler, callingCheck, localizedOptionAddToParty.GetSafeLocalizedString(), addToPartyConfiguration);
            }
            if (toggleRemoveFromParty)
            {
                AddDialogueSpawnOptionForConfiguration(ref interactActions, playerStateHandler, callingCheck, localizedOptionRemoveFromParty.GetSafeLocalizedString(), removeFromPartyConfiguration);
            }
            return interactActions;
        }
        
        public override List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedMessagePartyOptions.TableEntryReference,
                localizedOptionLeaderAdjust.TableEntryReference,
                localizedOptionAddToParty.TableEntryReference,
                localizedOptionRemoveFromParty.TableEntryReference,
            };
        }
    }
}
