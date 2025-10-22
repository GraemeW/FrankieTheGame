using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Utils.UI;

namespace Frankie.Combat.UI
{
    public class CombatLog : UIBox
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
        [Tooltip("Include {0} for name, {1} for points")][SerializeField] private string messageIncreaseHP = "{0} restored {1} HP.";
        [Tooltip("Include {0} for name, {1} for points")][SerializeField] private string messageDecreaseHP = "{0} was hit for {1} points.";
        [Tooltip("Include {0} for name, {1} for points")][SerializeField] private string messageIncreaseAP = "{0} restored {1} HP.";
        [Tooltip("Include {0} for name, {1} for points")][SerializeField] private string messageDecreaseAP = "{0} was drained for {1} AP.";
        [Tooltip("Include {0} for name")][SerializeField] private string messageDead = "{0} was knocked unconscious.";
        [Tooltip("Include {0} for name")][SerializeField] private string messageResurrected = "{0} gained the will to fight again.";

        // State
        private float combatLogDelay;
        private string stringToPrint = "";
        private string stringPrinted = "";
        private readonly List<CombatParticipant> combatParticipants = new();
        private bool isMarqueeActive = true;
        private Coroutine marquee;

        private void Awake()
        {
            combatLogDelay = delayBetweenCharactersSlowDown;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            BattleEventBus<BattleSequenceProcessedEvent>.SubscribeToEvent(ParseBattleSequence);
            BattleEventBus<StateAlteredInfo>.SubscribeToEvent(ParseCombatParticipantState);
            marquee = StartCoroutine(MarqueeScroll());
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            BattleEventBus<BattleSequenceProcessedEvent>.UnsubscribeFromEvent(ParseBattleSequence);
            BattleEventBus<StateAlteredInfo>.UnsubscribeFromEvent(ParseCombatParticipantState);
            StopCoroutine(marquee);
        }

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

        private void ParseBattleSequence(BattleSequenceProcessedEvent battleSequenceProcessedEvent)
        {
            if (battleSequenceProcessedEvent.battleEventType == BattleEventType.BattleExit) { isMarqueeActive = false; }
            
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
            if (stateAlteredInfo.stateAlteredType == StateAlteredType.IncreaseHP)
            {
                float points = stateAlteredInfo.points;
                combatText = string.Format(messageIncreaseHP, combatParticipantName, Mathf.RoundToInt(points).ToString());
            }
            else if (stateAlteredInfo.stateAlteredType == StateAlteredType.DecreaseHP)
            {
                float points = stateAlteredInfo.points;
                combatText = string.Format(messageDecreaseHP, combatParticipantName, Mathf.RoundToInt(points).ToString());
            }
            else if (stateAlteredInfo.stateAlteredType == StateAlteredType.IncreaseAP)
            {
                float points = stateAlteredInfo.points;
                combatText = string.Format(messageIncreaseAP, combatParticipantName, Mathf.RoundToInt(points).ToString());
            }
            else if (stateAlteredInfo.stateAlteredType == StateAlteredType.DecreaseAP)
            {
                float points = stateAlteredInfo.points;
                combatText = string.Format(messageDecreaseAP, combatParticipantName, Mathf.RoundToInt(points).ToString());
            }
            else if (stateAlteredInfo.stateAlteredType == StateAlteredType.Dead)
            {
                combatText = string.Format(messageDead, combatParticipantName);
            }
            else if (stateAlteredInfo.stateAlteredType == StateAlteredType.Resurrected)
            {
                combatText = string.Format(messageResurrected, combatParticipantName);
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
    }
}
