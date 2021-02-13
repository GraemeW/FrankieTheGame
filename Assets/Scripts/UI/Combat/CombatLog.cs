using Frankie.Dialogue.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Combat.UI
{
    public class CombatLog : DialogueBox
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

        // Cached References
        BattleController battleController = null;

        protected override void Awake()
        {
            base.Awake();
            battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();

            combatLogDelay = delayBetweenCharactersSlowDown;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            battleController.battleSequenceProcessed += ParseBattleSequence;
            ToggleCombatParticipantListeners(true);
            marquee = StartCoroutine(MarqueeScroll());
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            battleController.battleSequenceProcessed -= ParseBattleSequence;
            ToggleCombatParticipantListeners(false);
            StopCoroutine(marquee);
        }

        protected override void Update()
        {
            // Unused for combat log -- continuously marquee
        }

        private void ToggleCombatParticipantListeners(bool enable)
        {
            if (combatParticipants.Count > 0)
            {
                foreach (CombatParticipant combatParticipant in combatParticipants)
                {
                    if (enable) { combatParticipant.stateAltered += ParseCombatParticipantState; }
                    else { combatParticipant.stateAltered -= ParseCombatParticipantState; }
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

        private void ParseBattleSequence(BattleSequence battleSequence)
        {
            if (battleSequence.sender == null || battleSequence.recipient == null || battleSequence.skill == null) { return; }

            string combatText = battleSequence.sender.GetCombatName()
                + " used " + battleSequence.skill.name
                + " on " + battleSequence.recipient.GetCombatName() + ".";

            stringToPrint += "  " + combatText;
        }

        private void ParseCombatParticipantState(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            string combatText = "";
            if (stateAlteredData.stateAlteredType == StateAlteredType.IncreaseHP)
            {
                float points = stateAlteredData.points;
                combatText = string.Format(messageIncreaseHP, combatParticipant.GetCombatName(), Mathf.RoundToInt(points).ToString()); 
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.DecreaseHP)
            {
                float points = stateAlteredData.points;
                combatText = string.Format(messageDecreaseHP, combatParticipant.GetCombatName(), Mathf.RoundToInt(points).ToString());
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.IncreaseAP)
            {
                float points = stateAlteredData.points;
                combatText = string.Format(messageIncreaseAP, combatParticipant.GetCombatName(), Mathf.RoundToInt(points).ToString());
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.DecreaseAP)
            {
                float points = stateAlteredData.points;
                combatText = string.Format(messageDecreaseAP, combatParticipant.GetCombatName(), Mathf.RoundToInt(points).ToString());
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.Dead)
            {
                combatText = string.Format(messageDead, combatParticipant.GetCombatName());
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.Resurrected)
            {
                combatText = string.Format(messageResurrected, combatParticipant.GetCombatName());
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