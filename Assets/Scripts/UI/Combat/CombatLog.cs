using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Frankie.Utils.Localization;
using UnityEngine;
using Frankie.Utils.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Frankie.Combat.UI
{
    public class CombatLog : UIBox, ILocalizable
    {
        // Tunables
        [Header("Presentation")]
        [SerializeField] private int maxPrintedCharacters = 200; // Note:  real restrictions dictated by window width since clips to mask
        [SerializeField] private int speedUpDelayOnCharacterCount = 400;
        [SerializeField] private int slowDownDelayOnCharacterCount = 200;
        [SerializeField] private float delayBetweenCharactersSlowDown = 0.02f;
        [SerializeField] private float delayBetweenCharactersSpedUp = 0.01f;
        [SerializeField] private float delayBetweenCharactersNoNewMessage = 0.25f;
        [SerializeField] private SimpleTextLink textLink;
        [Header("Messages")]
        [Header("Include {0} for name, {1} for points")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageIncreaseHP;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageDecreaseHP;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageIncreaseAP;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageDecreaseAP;
        [Header("Include {0} for name")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageDead;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageResurrected;

        // State
        private float combatLogDelay;
        private string stringToPrint = "";
        private string stringPrinted = "";
        private readonly List<CombatParticipant> combatParticipants = new();
        private bool isMarqueeActive = true;
        private Coroutine marquee;

        #region UnityMethods
        private void Awake()
        {
            combatLogDelay = delayBetweenCharactersSlowDown;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(HandleBattleStateChangedEvent);
            BattleEventBus<BattleSequenceProcessedEvent>.SubscribeToEvent(ParseBattleSequence);
            BattleEventBus<StateAlteredInfo>.SubscribeToEvent(ParseCombatParticipantState);
            marquee = StartCoroutine(MarqueeScroll());
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(HandleBattleStateChangedEvent);
            BattleEventBus<BattleSequenceProcessedEvent>.UnsubscribeFromEvent(ParseBattleSequence);
            BattleEventBus<StateAlteredInfo>.UnsubscribeFromEvent(ParseCombatParticipantState);
            StopCoroutine(marquee);
        }
        #endregion

        #region PublicMethods
        public void AddCombatListener(CombatParticipant combatParticipant)
        {
            if (!combatParticipants.Contains(combatParticipant))
            {
                combatParticipants.Add(combatParticipant);
            }
        }

        public void AddCombatLogText(string text)
        {
            stringToPrint += text;
        }
        
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedMessageIncreaseHP.TableEntryReference,
                localizedMessageDecreaseHP.TableEntryReference,
                localizedMessageIncreaseAP.TableEntryReference,
                localizedMessageDecreaseAP.TableEntryReference,
                localizedMessageDead.TableEntryReference,
                localizedMessageResurrected.TableEntryReference
            };
        }
        #endregion

        #region PrivateMethods
        private IEnumerator MarqueeScroll()
        {
            while (isMarqueeActive)
            {
                if (stringPrinted.Length > maxPrintedCharacters)
                {
                    stringPrinted = stringPrinted.Remove(0, 1);
                }

                if (!string.IsNullOrEmpty(stringToPrint))
                {
                    stringPrinted += stringToPrint[0];
                    stringToPrint = stringToPrint.Remove(0, 1);
                    UpdateDelayTime();
                }
                else
                {
                    stringPrinted += ". ";
                    combatLogDelay = delayBetweenCharactersNoNewMessage;
                }
                textLink.Setup(stringPrinted);
                yield return new WaitForSeconds(combatLogDelay);
            }
        }

        private void HandleBattleStateChangedEvent(BattleStateChangedEvent battleStateChangedEvent)
        {
            isMarqueeActive = battleStateChangedEvent.battleState == BattleState.Combat;
        }

        private void ParseBattleSequence(BattleSequenceProcessedEvent battleSequenceProcessedEvent)
        {
            BattleSequence battleSequence = battleSequenceProcessedEvent.battleSequence;

            BattleActionData battleActionData = battleSequence.battleActionData;
            if (battleActionData == null || (battleSequence.battleActionSuper == null)) { return; }
            if (battleActionData.GetSender() == null || !battleActionData.HasTargets()) { return; }

            string recipientNames = string.Join(", ", battleActionData.GetTargets().Select(x => x.combatParticipant.GetCombatName()));
            string combatText = battleActionData.GetSender().GetCombatName()
                + " used " + battleSequence.battleActionSuper.GetName()
                + " on " + recipientNames + ".";

            stringToPrint += "  " + combatText;
        }

        private void ParseCombatParticipantState(StateAlteredInfo stateAlteredInfo)
        {
            string combatText = "";
            string combatParticipantName = stateAlteredInfo.combatParticipant.GetCombatName();
            switch (stateAlteredInfo.stateAlteredType)
            {
                case StateAlteredType.IncreaseHP:
                {
                    float points = stateAlteredInfo.points;
                    combatText = string.Format(localizedMessageIncreaseHP.GetSafeLocalizedString(), combatParticipantName, Mathf.RoundToInt(points).ToString());
                    break;
                }
                case StateAlteredType.DecreaseHP:
                {
                    float points = stateAlteredInfo.points;
                    combatText = string.Format(localizedMessageDecreaseHP.GetSafeLocalizedString(), combatParticipantName, Mathf.RoundToInt(points).ToString());
                    break;
                }
                case StateAlteredType.IncreaseAP:
                {
                    float points = stateAlteredInfo.points;
                    combatText = string.Format(localizedMessageIncreaseAP.GetSafeLocalizedString(), combatParticipantName, Mathf.RoundToInt(points).ToString());
                    break;
                }
                case StateAlteredType.DecreaseAP:
                {
                    float points = stateAlteredInfo.points;
                    combatText = string.Format(localizedMessageDecreaseAP.GetSafeLocalizedString(), combatParticipantName, Mathf.RoundToInt(points).ToString());
                    break;
                }
                case StateAlteredType.Dead:
                {
                    combatText = string.Format(localizedMessageDead.GetSafeLocalizedString(), combatParticipantName);
                    break;
                }
                case StateAlteredType.Resurrected:
                {
                    combatText = string.Format(localizedMessageResurrected.GetSafeLocalizedString(), combatParticipantName);
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(combatText))
            {
                stringToPrint += "  " + combatText;
            }
        }

        private void UpdateDelayTime()
        {
            if (stringToPrint.Length > speedUpDelayOnCharacterCount)
            {
                combatLogDelay = delayBetweenCharactersSpedUp;
            }
            else if (stringToPrint.Length < slowDownDelayOnCharacterCount)
            {
                combatLogDelay = delayBetweenCharactersSlowDown;
            }
        }
        #endregion
    }
}
