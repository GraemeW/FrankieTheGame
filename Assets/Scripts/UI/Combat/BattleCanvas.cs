using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Frankie.Speech.UI;
using UnityEngine.UI;
using System;
using Frankie.Stats;

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
        [SerializeField] Image backgroundFill = null;
        [SerializeField] MovingBackgroundProperties defaultMovingBackgroundProperties;
        [SerializeField] Transform infoChooseParent = null;
        [SerializeField] GameObject dialogueBoxPrefab = null;
        [SerializeField] CombatLog combatLog = null;
        [SerializeField] CombatOptions combatOptions = null;
        [SerializeField] SkillSelectionUI skillSelection = null;

        [Header("Messages")]
        [Tooltip("Include {0} for enemy name")][SerializeField] string messageEncounterSingle = "You have encountered {0}.";
        [Tooltip("Include {0} for enemy name")][SerializeField] string messageEncounterMultiple = "You have encountered {0} and its cohort.";
        [Tooltip("Called after encounter message")] [SerializeField] string messageEncounterPreHype = "What do you want to do?";
        [Tooltip("Include {0} for experience value")][SerializeField] string messageGainedExperience = "Your party has gained {0} experience.";
        [Tooltip("Include {0} for character name, {1} for level")][SerializeField] string messageCharacterLevelUp = "{0} has leveled up to level {1}!";
        [Tooltip("Include {0} for stat name, {1} for value")] [SerializeField] string messageCharacterStatGained = "{0} has increased by {1}.";
        [SerializeField] string messageBattleCompleteWon = "You Won!  Congratulations!";
        [SerializeField] string messageBattleCompleteLost = "You lost.  Whoops!";
        [SerializeField] string messageBattleCompleteRan = "You ran away.";

        // State
        BattleState lastBattleState = BattleState.Inactive;
        Queue<Action> queuedUISequences = new Queue<Action>();
        List<CharacterLevelUpSheetPair> queuedLevelUps = new List<CharacterLevelUpSheetPair>();
        bool busyWithSerialAction = false;

        // Cached References
        Party party = null;
        BattleController battleController = null;

        // Static
        private static string DIALOGUE_CALLBACK_INTRO_COMPLETE = "INTRO_COMPLETE";
        private static string DIALOGUE_CALLBACK_OUTRO_COMPLETE = "OUTRO_COMPLETE";
        private static string DIALOGUE_CALLBACK_SERIAL_ACTION_COMPLETE = "SERIAL_ACTION_COMPLETE";

        // Data Structures
        public struct CharacterLevelUpSheetPair
        {
            public CombatParticipant character;
            public int level;
            public List<Tuple<string, int>> statNameValuePairs;
        }

        private void Awake()
        {
            battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();
            party = GameObject.FindGameObjectWithTag("Player").GetComponent<Party>();

            combatOptions.Setup(battleController, this, party);

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

        private void Update()
        {
            if (!busyWithSerialAction && queuedUISequences.Count != 0)
            {
                Action nextUISequence = queuedUISequences.Dequeue();
                StartSerialAction(nextUISequence);
            }
        }

        public void Setup(BattleState state)
        {
            if (state == lastBattleState) { return; } // Only act on state changes
            lastBattleState = state;

            if (state == BattleState.Intro)
            {
                SetupPlayerCharacters(battleController.GetCharacters());
                SetupEnemies(battleController.GetEnemies());
                SetupBackgroundFill(battleController.GetEnemies());
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
                battleController.SetSelectedCharacter(null);
            }
            else if (state == BattleState.Outro)
            {
                combatLog.gameObject.SetActive(false);
                skillSelection.gameObject.SetActive(false);
                queuedUISequences.Enqueue(SetupExperienceMessage);
                queuedUISequences.Enqueue(SetupExitMessage);
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
                character.characterLevelUp += HandleLevelUp; 
            }
        }

        private void SetupEnemies(IEnumerable enemies)
        {
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
        }

        private void SetupBackgroundFill(List<CombatParticipant> enemies)
        {
            int enemyIndex = UnityEngine.Random.Range(0, enemies.Count);
            MovingBackgroundProperties movingBackgroundProperties = enemies[enemyIndex].GetMovingBackgroundProperties();
            if (movingBackgroundProperties.tileSpriteImage == null || movingBackgroundProperties.shaderMaterial == null)
            {
                backgroundFill.sprite = defaultMovingBackgroundProperties.tileSpriteImage;
                backgroundFill.material = defaultMovingBackgroundProperties.shaderMaterial;
            }
            else
            {
                backgroundFill.sprite = movingBackgroundProperties.tileSpriteImage;
                backgroundFill.material = movingBackgroundProperties.shaderMaterial;
            }
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
            dialogueBox.AddText(entryMessage);
            dialogueBox.AddPageBreak();
            dialogueBox.AddText(messageEncounterPreHype);
            dialogueBox.SetGlobalCallbacks(battleController);
            dialogueBox.SetDisableCallback(this, DIALOGUE_CALLBACK_INTRO_COMPLETE);
        }

        private void StartSerialAction(Action action)
        {
            // !! It is the responsibility of the called action to reset busyWithSerialAction toggle !!
            busyWithSerialAction = true;
            action.Invoke();
        }

        private void HandleLevelUp(CombatParticipant character, int level, List<Tuple<string, int>> statNameValuePairs)
        {
            CharacterLevelUpSheetPair characterLevelUpSheetPair = new CharacterLevelUpSheetPair
            {
                character = character,
                level = level,
                statNameValuePairs = statNameValuePairs
            };
            queuedLevelUps.Add(characterLevelUpSheetPair);
        }

        private void SetupExperienceMessage()
        {
            if (battleController.GetBattleOutcome() != BattleOutcome.Won) { busyWithSerialAction = false; return; }

            GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, infoChooseParent);
            DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
            dialogueBox.AddText(string.Format(messageGainedExperience, battleController.GetBattleExperienceReward().ToString()));

            foreach (CharacterLevelUpSheetPair characterLevelUpSheetPair in queuedLevelUps)
            {
                dialogueBox.AddPageBreak();

                dialogueBox.AddText(string.Format(messageCharacterLevelUp, characterLevelUpSheetPair.character.GetCombatName(), characterLevelUpSheetPair.level.ToString()));
                int pageClearReset = 0;

                foreach (Tuple<string, int> statNameValuePair in characterLevelUpSheetPair.statNameValuePairs)
                {
                    if (pageClearReset > 2)
                    {
                        dialogueBox.AddPageBreak();
                        pageClearReset = 0;
                    }

                    dialogueBox.AddText(string.Format(messageCharacterStatGained, statNameValuePair.Item1, statNameValuePair.Item2.ToString()));
                    pageClearReset++;
                }

                characterLevelUpSheetPair.character.characterLevelUp -= HandleLevelUp;
                    // Unsubscribe to messages -- not the cleanest location, but the only one available
            }
            dialogueBox.SetGlobalCallbacks(battleController);
            dialogueBox.SetDisableCallback(this, DIALOGUE_CALLBACK_SERIAL_ACTION_COMPLETE);
        }

        private void SetupExitMessage()
        {
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
            dialogueBox.AddText(exitMessage);
            dialogueBox.SetGlobalCallbacks(battleController);
            dialogueBox.SetDisableCallback(this, DIALOGUE_CALLBACK_OUTRO_COMPLETE);
            battleController.SetHandleLevelUp(false);
        }

        public DialogueBox SetupRunFailureMessage()
        {
            GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, infoChooseParent);
            DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
            dialogueBox.AddText("Failed to run away.");
            dialogueBox.SetGlobalCallbacks(battleController);

            return dialogueBox;
        }

        public void HandleDialogueCallback(DialogueBox dialogueBox, string callbackMessage)
        {
            if (callbackMessage == DIALOGUE_CALLBACK_INTRO_COMPLETE)
            {
                battleController.SetBattleState(BattleState.PreCombat);
            }
            else if (callbackMessage == DIALOGUE_CALLBACK_OUTRO_COMPLETE)
            {
                busyWithSerialAction = false;
                battleController.SetBattleState(BattleState.Complete);
            }
            else if (callbackMessage == DIALOGUE_CALLBACK_SERIAL_ACTION_COMPLETE)
            {
                busyWithSerialAction = false;
            }
        }
    }
}
