using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Frankie.Speech.UI;
using UnityEngine.UI;

namespace Frankie.Combat.UI
{
    public class BattleCanvas : MonoBehaviour, IDialogueBoxCallbackReceiver
    {
        // Tunables
        [Header("Parents and Prefabs")]
        [SerializeField] Transform playerPanelParent = null;
        [SerializeField] GameObject characterSlidePrefab = null;
        [SerializeField] Transform frontRowParent = null;
        [SerializeField] Transform backRowParent = null;
        [SerializeField] GameObject enemyPrefab = null;
        [SerializeField] Transform infoChooseParent = null;
        [SerializeField] GameObject dialogueBoxPrefab = null;
        [SerializeField] CombatLog combatLog = null;
        [SerializeField] CombatOptions combatOptions = null;
        [SerializeField] SkillSelection skillSelection = null;

        [Header("Messages")]
        [Tooltip("Include {0} for enemy name")][SerializeField] string messageEncounterSingle = "You have encountered {0}.";
        [Tooltip("Include {0} for enemy name")][SerializeField] string messageEncounterMultiple = "You have encountered {0} and its cohort.";
        [Tooltip("Called after encounter message")] [SerializeField] string messageEncounterPreHype = "What do you want to do?";
        [SerializeField] string messageBattleCompleteWon = "You Won!  Congratulations!";
        [SerializeField] string messageBattleCompleteLost = "You lost.  Whoops!";
        [SerializeField] string messageBattleCompleteRan = "You ran away.";

        // Cached References
        BattleController battleController = null;

        // Static
        private static string DIALOGUE_CALLBACK_INTRO_COMPLETE = "INTRO_COMPLETE";
        private static string DIALOGUE_CALLBACK_OUTRO_COMPLETE = "OUTRO_COMPLETE";

        private void Awake()
        {
            battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();
            ClearBattleCanvas();
        }

        private void OnEnable()
        {
            battleController.battleStateChanged += Setup;
        }

        private void OnDisable()
        {
            battleController.battleStateChanged -= Setup;
        }

        private void OnDestroy()
        {
            ClearBattleCanvas();
        }

        public void Setup(BattleState state)
        {
            if (state == BattleState.Intro)
            {
                SetupPlayerCharacters(battleController.GetCharacters());
                SetupEnemies(battleController.GetEnemies());
                SetupEntryMessage(battleController.GetEnemies());
            }
            else if (state == BattleState.PreCombat)
            {
                skillSelection.gameObject.SetActive(false);
                combatOptions.gameObject.SetActive(true);
            }
            else if (state == BattleState.Combat)
            {
                combatLog.AddCombatLogText("  Combat Started . . . ");
                combatLog.gameObject.SetActive(true);
                skillSelection.gameObject.SetActive(true);
            }
            else if (state == BattleState.Outro)
            {
                combatLog.gameObject.SetActive(false);
                skillSelection.gameObject.SetActive(false);
                SetupExitMessage();
            }
            else if (state == BattleState.LevelUp)
            {
                // TODO:  Handle level up ~ redundant of outro, but with extras
                // alternatively, can handle as a follow-up to outro (though I don't love the chain)
            }
        }

        private void ClearBattleCanvas()
        {
            foreach (Transform child in playerPanelParent)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in frontRowParent)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in backRowParent)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in infoChooseParent)
            {
                if (child.gameObject != combatOptions || child.gameObject != skillSelection) { continue; }
                Destroy(child.gameObject);
            }
        }

        private void SetupPlayerCharacters(IEnumerable characters)
        {
            bool firstCharacterToggle = false;
            foreach (CombatParticipant character in characters)
            {
                GameObject characterObject = Instantiate(characterSlidePrefab, playerPanelParent);
                CharacterSlide characterSlide = characterObject.GetComponent<CharacterSlide>();
                characterSlide.SetCombatParticipant(character);
                combatLog.AddCombatListener(character);

                if (!firstCharacterToggle)
                {
                    characterSlide.GetComponent<Button>().Select();
                    battleController.SetSelectedCharacter(character);
                    firstCharacterToggle = true;
                }
            }
        }

        private void SetupEnemies(IEnumerable enemies)
        {
            // TODO:  Add support for two different types of presentation -- two-row small format, one-row large format

            // State
            List<EnemySlide> enemySlides = new List<EnemySlide>();

            foreach (CombatParticipant enemy in enemies)
            {
                Transform parentSpawn = null;
                if (frontRowParent.childCount - 1 > backRowParent.childCount)
                {
                    parentSpawn = backRowParent;
                }
                else
                {
                    parentSpawn = frontRowParent;
                }
                GameObject enemyObject = Instantiate(enemyPrefab, parentSpawn);
                EnemySlide enemySlide = enemyObject.GetComponent<EnemySlide>();
                enemySlide.SetCombatParticipant(enemy);
                enemySlides.Add(enemySlide);
                combatLog.AddCombatListener(enemy);
            }
            skillSelection.SetEnemySlides(enemySlides);
        }

        private void SetupEntryMessage(List<CombatParticipant> enemies)
        {
            CombatParticipant enemy = enemies.FirstOrDefault();
            GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, infoChooseParent);

            string entryMessage;
            if (enemies.Count > 1)
            {
                entryMessage = string.Format(messageEncounterMultiple, enemy.GetCombatName());
            }
            else
            {
                entryMessage = string.Format(messageEncounterSingle, enemy.GetCombatName());
            }

            DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
            dialogueBox.AddSimpleText(entryMessage);
            dialogueBox.AddPageBreak();
            dialogueBox.AddSimpleText(messageEncounterPreHype);
            dialogueBox.SetDisableCallback(this, DIALOGUE_CALLBACK_INTRO_COMPLETE);
        }

        private void SetupExitMessage()
        {
            // TODO:  Handle other battle ending types
            // TODO:  Add experience message
            GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, infoChooseParent);

            string exitMessage = "";
            if (battleController.GetBattleOutcome() == BattleOutcome.Won)
            {
                exitMessage = messageBattleCompleteWon;
            }
            else if (battleController.GetBattleOutcome() == BattleOutcome.Lost)
            {
                exitMessage = messageBattleCompleteLost;
            }
            else if (battleController.GetBattleOutcome() == BattleOutcome.Ran)
            {
                exitMessage = messageBattleCompleteRan;
            }

            DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
            dialogueBox.AddSimpleText(exitMessage);
            dialogueBox.SetDisableCallback(this, DIALOGUE_CALLBACK_OUTRO_COMPLETE);
        }

        public void HandleDialogueCallback(string callbackMessage)
        {
            if (callbackMessage == DIALOGUE_CALLBACK_INTRO_COMPLETE)
            {
                battleController.SetBattleState(BattleState.PreCombat);
            }
            else if (callbackMessage == DIALOGUE_CALLBACK_OUTRO_COMPLETE)
            {
                battleController.SetBattleState(BattleState.Complete);
            }
        }
    }
}
