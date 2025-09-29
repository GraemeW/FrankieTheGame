using Frankie.Utils.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Combat.UI
{
    public class CombatLog : UIBox
    {
        // Tunables
        [Header("Presentation")]
        [SerializeField] int maxPrintedCharacters = 200; // Note:  real restrictions dictated by window width since clips to mask
        [SerializeField] int speedUpDelayOnCharacterCount = 400;
        [SerializeField] int slowDownDelayOnCharacterCount = 200;
        [SerializeField] float delayBetweenCharactersSlowDown = 0.02f;
        [SerializeField] float delayBetweenCharactersSpedUp = 0.01f;
        [SerializeField] float delayBetweenCharactersNoNewMessage = 0.25f;
        [SerializeField] SimpleTextLink textLink = null;
        [Header("Messages")]
        [Tooltip("Include {0} for name, {1} for points")] [SerializeField] string messageIncreaseHP = "{0} restored {1} HP.";
        [Tooltip("Include {0} for name, {1} for points")] [SerializeField] string messageDecreaseHP = "{0} was hit for {1} points.";
        [Tooltip("Include {0} for name, {1} for points")] [SerializeField] string messageIncreaseAP = "{0} restored {1} HP.";
        [Tooltip("Include {0} for name, {1} for points")] [SerializeField] string messageDecreaseAP = "{0} was drained for {1} AP.";
        [Tooltip("Include {0} for name")] [SerializeField] string messageDead = "{0} was knocked unconcious.";
        [Tooltip("Include {0} for name")] [SerializeField] string messageResurrected = "{0} gained the will to fight again.";

        // State
        float combatLogDelay = 0f;
        string stringToPrint = "";
        string stringPrinted = "";
        List<CombatParticipant> combatParticipants = new List<CombatParticipant>();
        Coroutine marquee = null;

        private void Awake()
        {
            combatLogDelay = delayBetweenCharactersSlowDown;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            BattleEventBus<BattleEnterEvent>.SubscribeToEvent(SetupCombatParticipants);
            BattleEventBus<BattleSequenceProcessedEvent>.SubscribeToEvent(ParseBattleSequence);
            ToggleCombatParticipantListeners(true);
            marquee = StartCoroutine(MarqueeScroll());
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            BattleEventBus<BattleEnterEvent>.UnsubscribeFromEvent(SetupCombatParticipants);
            BattleEventBus<BattleSequenceProcessedEvent>.UnsubscribeFromEvent(ParseBattleSequence);
            ToggleCombatParticipantListeners(false);
            StopCoroutine(marquee);
        }

        private void SetupCombatParticipants(BattleEnterEvent battleEnterEvent)
        {
            if (combatParticipants == null) { combatParticipants = new List<CombatParticipant>(); }
            combatParticipants.Clear();

            foreach (BattleEntity battleEntity in battleEnterEvent.playerEntities)
            {
                combatParticipants.Add(battleEntity.combatParticipant);
            }
            foreach (BattleEntity battleEntity in battleEnterEvent.enemyEntities)
            {
                combatParticipants.Add(battleEntity.combatParticipant);
            }
        }

        private void ToggleCombatParticipantListeners(bool enable)
        {
            if (combatParticipants.Count > 0)
            {
                foreach (CombatParticipant combatParticipant in combatParticipants)
                {
                    if (enable) { combatParticipant.SubscribeToStateUpdates(ParseCombatParticipantState); }
                    else { combatParticipant.UnsubscribeToStateUpdates(ParseCombatParticipantState); }
                }
            }
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
            while (true)
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
            BattleSequence battleSequence = battleSequenceProcessedEvent.battleSequence;

            BattleActionData battleActionData = battleSequence.battleActionData;
            if (battleActionData == null || (battleSequence.battleActionSuper == null)) { return; }
            if (battleActionData.GetSender() == null || battleActionData.targetCount == 0) { return; }

            string recipientNames = string.Join(", ", battleActionData.GetTargets().Select(x => x.combatParticipant.GetCombatName()));
            string combatText = battleActionData.GetSender().GetCombatName()
                + " used " + battleSequence.battleActionSuper.GetName()
                + " on " + recipientNames + ".";

            stringToPrint += "  " + combatText;
        }

        private void ParseCombatParticipantState(StateAlteredEvent stateAlteredData)
        {
            string combatText = "";
            string combatParticipantName = stateAlteredData.combatParticipant.GetCombatName();
            if (stateAlteredData.stateAlteredType == StateAlteredType.IncreaseHP)
            {
                float points = stateAlteredData.points;
                combatText = string.Format(messageIncreaseHP, combatParticipantName, Mathf.RoundToInt(points).ToString());
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.DecreaseHP)
            {
                float points = stateAlteredData.points;
                combatText = string.Format(messageDecreaseHP, combatParticipantName, Mathf.RoundToInt(points).ToString());
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.IncreaseAP)
            {
                float points = stateAlteredData.points;
                combatText = string.Format(messageIncreaseAP, combatParticipantName, Mathf.RoundToInt(points).ToString());
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.DecreaseAP)
            {
                float points = stateAlteredData.points;
                combatText = string.Format(messageDecreaseAP, combatParticipantName, Mathf.RoundToInt(points).ToString());
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.Dead)
            {
                combatText = string.Format(messageDead, combatParticipantName);
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.Resurrected)
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