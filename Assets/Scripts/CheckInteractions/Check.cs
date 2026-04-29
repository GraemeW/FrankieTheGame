using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Utils;

namespace Frankie.Control
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Collider2D))]
    public class Check : CheckBase, ILocalizable
    {
        [Header("Base Check Behaviour")]
        [SerializeField] private CheckType checkType = CheckType.Simple;
        [SerializeField] protected InteractionEvent checkInteraction;
        [Header("Message Behaviour")]
        [SerializeField][Tooltip("Otherwise, checks at end of interaction")] private bool checkAtStartOfInteraction = false;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] protected LocalizedString localizedCheckMessage;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedDefaultPartyLeaderName;
        [Header("Choice Behaviour")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageAccept;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageReject;
        [SerializeField][Tooltip("Optional action on reject choice")] private InteractionEvent rejectInteraction;
        [Header("TODO: Remove these after copying")]
        [SerializeField][Tooltip("Use {0} for party leader")] protected string checkMessage = "{0} has checked this object";
        [SerializeField] private string defaultPartyLeaderName = "Frankie";
        [SerializeField] private string messageAccept = "OK!";
        [SerializeField] private string messageReject = "Nah";
        
        // Const Failsafe
        private const string _failsafePartyLeaderName = "Frankie";
        private const string _failsafeMessageAccept = "OK!";
        private const string _failsafeMessageReject = "Nah";

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
        
        public LocalizationTableType localizationTableType { get; set; } = LocalizationTableType.ChecksWorldObjects;

        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedCheckMessage.TableEntryReference,
                localizedDefaultPartyLeaderName.TableEntryReference,
                localizedMessageAccept.TableEntryReference,
                localizedMessageReject.TableEntryReference
            };
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
            if (localizedCheckMessage.IsEmpty) { return false; }
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                string partyLeaderName = playerStateHandler.GetParty().GetPartyLeaderName();
                if (string.IsNullOrWhiteSpace(partyLeaderName)) { partyLeaderName = !localizedDefaultPartyLeaderName.IsEmpty ?  localizedDefaultPartyLeaderName.GetLocalizedString() : _failsafePartyLeaderName; }

                playerStateHandler.EnterDialogue(string.Format(localizedCheckMessage.GetLocalizedString(), partyLeaderName));
                
                if (checkAtStartOfInteraction) { checkInteraction?.Invoke(playerStateHandler); }
                else { playerStateHandler.SetPostDialogueCallbackActions(checkInteraction); }
            }
            return true;
        }

        private bool ChoiceConfirmationCheck(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (localizedCheckMessage.IsEmpty) { return false; }
            if (!IsInRange(playerController)) { return false; }

            string localMessageAccept = !localizedMessageAccept.IsEmpty ? localizedMessageAccept.GetLocalizedString() : _failsafeMessageAccept;
            string localMessageReject = !localizedMessageReject.IsEmpty ? localizedMessageReject.GetLocalizedString() : _failsafeMessageReject;
            if (inputType == matchType)
            {
                var interactActions = new List<ChoiceActionPair>
                {
                    new(localMessageAccept, () => checkInteraction.Invoke(playerStateHandler)),
                    new(localMessageReject, () => rejectInteraction.Invoke(playerStateHandler))
                };

                string partyLeaderName = playerStateHandler.GetParty().GetPartyLeaderName();
                if (string.IsNullOrWhiteSpace(partyLeaderName)) { partyLeaderName = !localizedDefaultPartyLeaderName.IsEmpty ?  localizedDefaultPartyLeaderName.GetLocalizedString() : _failsafePartyLeaderName; }
                
                playerStateHandler.EnterDialogue(string.Format(localizedCheckMessage.GetLocalizedString(), partyLeaderName), interactActions);
            }
            return true;
        }
        #endregion

        public void TempCreateCheckEntries()
        {
            string keyStem;
            string key;
            TableEntryReference tableEntryReference;
            
            keyStem = nameof(localizedCheckMessage).Replace("localized", "");
            key = LocalizationTool.GenerateKindaUniqueKey(GetType(), gameObject, keyStem);
            tableEntryReference = key;
            if (localizedCheckMessage == null || LocalizationTool.GetEnglishEntry(localizationTableType, localizedCheckMessage.TableEntryReference) != checkMessage)
            {
                LocalizationTool.AddUpdateEnglishEntry(localizationTableType, tableEntryReference, checkMessage);
                LocalizationTool.SafelyUpdateReference(localizationTableType, localizedCheckMessage, key);
            }
            
            keyStem = nameof(localizedDefaultPartyLeaderName).Replace("localized", "");
            key = LocalizationTool.GenerateKindaUniqueKey(GetType(), gameObject, keyStem);
            tableEntryReference = key;
            if (localizedDefaultPartyLeaderName == null || LocalizationTool.GetEnglishEntry(localizationTableType, localizedDefaultPartyLeaderName.TableEntryReference) != defaultPartyLeaderName)
            {
                LocalizationTool.AddUpdateEnglishEntry(localizationTableType, tableEntryReference, defaultPartyLeaderName);
                LocalizationTool.SafelyUpdateReference(localizationTableType, localizedDefaultPartyLeaderName, key);
            }
            
            keyStem = nameof(localizedMessageAccept).Replace("localized", "");
            key = LocalizationTool.GenerateKindaUniqueKey(GetType(), gameObject, keyStem);
            tableEntryReference = key;
            if (localizedMessageAccept == null || LocalizationTool.GetEnglishEntry(localizationTableType, localizedMessageAccept.TableEntryReference) != messageAccept)
            {
                LocalizationTool.AddUpdateEnglishEntry(localizationTableType, tableEntryReference, messageAccept);
                LocalizationTool.SafelyUpdateReference(localizationTableType, localizedMessageAccept, key);
            }
            
            keyStem = nameof(localizedMessageReject).Replace("localized", "");
            key = LocalizationTool.GenerateKindaUniqueKey(GetType(), gameObject, keyStem);
            tableEntryReference = key;
            if (localizedMessageReject == null || LocalizationTool.GetEnglishEntry(localizationTableType, localizedMessageReject.TableEntryReference) != messageReject)
            {
                LocalizationTool.AddUpdateEnglishEntry(localizationTableType, tableEntryReference, messageReject);
                LocalizationTool.SafelyUpdateReference(localizationTableType, localizedMessageReject, key);
            }
        }
    }
}
