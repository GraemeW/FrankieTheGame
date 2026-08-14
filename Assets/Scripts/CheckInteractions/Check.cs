using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Utils;
using LowDefMustard.Control;
using LowDefMustard.Localization;
using Frankie.Core;
using Frankie.Stats;

namespace Frankie.Control
{
    [RequireComponent(typeof(Collider2D))]
    public class Check : CheckBase
    {
        [Header("Base Check Behaviour")]
        [SerializeField] private CheckType checkType = CheckType.Simple;
        [SerializeField] protected InteractionEvent checkInteraction;
        [Header("Message Behaviour")]
        [SerializeField][Tooltip("Otherwise, checks at end of interaction")] private bool checkAtStartOfInteraction = false;
        [Header("Note - {0} for party leader, {1} for check object name")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedCheckMessage;
        [Header("Choices")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageAccept;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageReject;
        [SerializeField][Tooltip("Optional action on reject choice")] private InteractionEvent rejectInteraction;
        
        // Const Failsafe
        private const string _failsafeMessageAccept = "OK!";
        private const string _failsafeMessageReject = "Nah";

        #region Interfaces
        public override bool HandleRaycast(PlayerStateMachine playerStateMachine, PlayerController playerController, ControllerInputType inputType, ControllerInputType matchType)
        {
            return checkType switch
            {
                CheckType.Simple => SimpleCheck(playerStateMachine, playerController, inputType, matchType),
                CheckType.Message => MessageCheck(playerStateMachine, playerController, inputType, matchType),
                CheckType.ChoiceConfirmation => ChoiceConfirmationCheck(playerStateMachine, playerController, inputType, matchType),
                _ => SimpleCheck(playerStateMachine, playerController, inputType, matchType),
            };
        }

        public override List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedCheckMessage.TableEntryReference,
                localizedMessageAccept.TableEntryReference,
                localizedMessageReject.TableEntryReference
            };
        }
        #endregion

        #region SpecificImplementation
        private bool SimpleCheck(PlayerStateMachine playerStateMachine, PlayerController playerController, ControllerInputType inputType, ControllerInputType matchType)
        {
            if (!IsInRange(playerController)) { return false; }
            if (inputType == matchType) { checkInteraction?.Invoke(playerStateMachine); }
            return true;
        }

        private bool MessageCheck(PlayerStateMachine playerStateMachine, PlayerController playerController, ControllerInputType inputType, ControllerInputType matchType)
        {
            if (localizedCheckMessage.IsEmpty) { return false; }
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                string partyLeaderName = playerStateMachine.GetParty().GetPartyLeaderName();
                if (string.IsNullOrWhiteSpace(partyLeaderName)) { partyLeaderName = defaultPartyLeaderName; }

                playerStateMachine.EnterDialogue(string.Format(localizedCheckMessage.GetSafeLocalizedString(), partyLeaderName, GetCheckObjectName()));
                
                if (checkAtStartOfInteraction) { checkInteraction?.Invoke(playerStateMachine); }
                else { playerStateMachine.SetPostDialogueCallbackActions(checkInteraction); }
            }
            return true;
        }

        private bool ChoiceConfirmationCheck(PlayerStateMachine playerStateMachine, PlayerController playerController, ControllerInputType inputType, ControllerInputType matchType)
        {
            if (localizedCheckMessage.IsEmpty) { return false; }
            if (!IsInRange(playerController)) { return false; }
            
            string localMessageAccept = localizedMessageAccept.GetSafeLocalizedString();
            if (string.IsNullOrWhiteSpace(localMessageAccept)) { localMessageAccept = _failsafeMessageAccept; }
            string localMessageReject = localizedMessageReject.GetSafeLocalizedString();
            if (string.IsNullOrWhiteSpace(localMessageReject)) { localMessageReject = _failsafeMessageReject; }
            if (inputType == matchType)
            {
                var interactActions = new List<ChoiceActionPair>
                {
                    new(localMessageAccept, () => checkInteraction.Invoke(playerStateMachine)),
                    new(localMessageReject, () => rejectInteraction.Invoke(playerStateMachine))
                };

                string partyLeaderName = playerStateMachine.GetParty().GetPartyLeaderName();
                if (string.IsNullOrWhiteSpace(partyLeaderName)) { partyLeaderName = defaultPartyLeaderName; }
                
                playerStateMachine.EnterDialogue(string.Format(localizedCheckMessage.GetSafeLocalizedString(), partyLeaderName, GetCheckObjectName()), interactActions);
            }
            return true;
        }

        private string GetCheckObjectName()
        {
            if (transform.parent == null) { return TryGetComponent(out BaseStats baseStats) ? CharacterProperties.GetCharacterDisplayName(baseStats) : name; }
            return transform.parent.TryGetComponent(out BaseStats parentBaseStats) ? CharacterProperties.GetCharacterDisplayName(parentBaseStats) : transform.parent.name;
        }
        #endregion
    }
}
