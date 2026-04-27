using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using Frankie.Utils;

namespace Frankie.Control
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Collider2D))]
    public class Check : CheckBase, ILocalizable
    {
        // Events
        [Header("Base Check Behaviour")]
        [SerializeField] private CheckType checkType = CheckType.Simple;
        [SerializeField] protected InteractionEvent checkInteraction;
        [Header("Message Behaviour")]
        [SerializeField][Tooltip("Otherwise, checks at end of interaction")] private bool checkAtStartOfInteraction = false;
        [SerializeField][Tooltip("Use {0} for party leader")] protected string checkMessage = "{0} has checked this object";
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] protected LocalizedString localizedCheckMessage;
        [SerializeField] private string defaultPartyLeaderName = "Frankie";
        [SerializeField] [SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedDefaultPartyLeaderName;
        [Header("Choice Behaviour")]
        [SerializeField] private string messageAccept = "OK!";
        [SerializeField] [SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageAccept;
        [SerializeField] private string messageReject = "Nah";
        [SerializeField] [SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageReject;
        [SerializeField][Tooltip("Optional action on reject choice")] private InteractionEvent rejectInteraction;

        #region Interfaces
        public override bool HandleRaycast(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            return checkType switch
            {
                CheckType.Simple => SimpleCheck(playerStateHandler, playerController, inputType, matchType),
                CheckType.Message => MessageCheck(playerStateHandler, playerController, inputType, matchType),
                CheckType.ChoiceConfirmation => ChoiceConfirmationCheck(playerStateHandler, playerController, inputType, matchType),
                _ => SimpleCheck(playerStateHandler, playerController, inputType, matchType),
            };
        }

        public void HandleDeletion()
        {
            // TODO:  Implement
        }
        
        protected void OnDestroy()
        {
            ILocalizable.TriggerOnDestroy(this);
        }
        #endregion

        #region SpecificImplementation
        private bool SimpleCheck(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (!IsInRange(playerController)) { return false; }
            if (inputType == matchType) { checkInteraction?.Invoke(playerStateHandler); }
            return true;
        }

        private bool MessageCheck(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (string.IsNullOrEmpty(checkMessage)) { return false; }
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                string partyLeaderName = playerStateHandler.GetParty().GetPartyLeaderName();
                if (string.IsNullOrWhiteSpace(partyLeaderName)) { partyLeaderName = defaultPartyLeaderName; }

                playerStateHandler.EnterDialogue(string.Format(checkMessage, partyLeaderName));
                
                if (checkAtStartOfInteraction) { checkInteraction?.Invoke(playerStateHandler); }
                else { playerStateHandler.SetPostDialogueCallbackActions(checkInteraction); }
            }
            return true;
        }

        private bool ChoiceConfirmationCheck(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (string.IsNullOrEmpty(checkMessage)) { return false; }
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                var interactActions = new List<ChoiceActionPair>
                {
                    new(messageAccept, () => checkInteraction.Invoke(playerStateHandler)),
                    new(messageReject, () => rejectInteraction.Invoke(playerStateHandler))
                };

                string partyLeaderName = playerStateHandler.GetParty().GetPartyLeaderName();
                playerStateHandler.EnterDialogue(string.Format(checkMessage, partyLeaderName), interactActions);
            }
            return true;
        }
        #endregion
    }
}
