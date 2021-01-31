using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Frankie.Dialogue.UI;
using static Frankie.Combat.BattleController;
using Frankie.Stats;
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
        [SerializeField] CombatOptions combatOptions = null;
        [SerializeField] SkillSelection skillSelection = null;

        // Cached References
        Party party = null;
        BattleController battleController = null;

        // State
        List<EnemySlide> enemySlides = new List<EnemySlide>();
        List<CharacterSlide> characterSlides = new List<CharacterSlide>();

        // Static
        private static string DIALOGUE_CALLBACK_INTRO_COMPLETE = "INTRO_COMPLETE";
        private static string DIALOGUE_CALLBACK_OUTRO_COMPLETE = "OUTRO_COMPLETE";

        private void Awake()
        {
            party = GameObject.FindGameObjectWithTag("Player").GetComponent<Party>();
        }

        private void OnEnable()
        {
            battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();
            battleController.battleStateChanged += Setup;
            SetupCharacterSlideButtons(true);
        }

        private void OnDisable()
        {
            battleController.battleStateChanged -= Setup;
            SetupCharacterSlideButtons(false);
            characterSlides.Clear();
            enemySlides.Clear();
            ClearBattleCanvas();
        }

        public void Setup(BattleState state)
        {
            if (state == BattleState.Intro)
            {
                ClearBattleCanvas();
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
                skillSelection.gameObject.SetActive(true);
            }
            else if (state == BattleState.Outro)
            {
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
                characterSlide.SetCharacter(character);
                characterSlides.Add(characterSlide);

                if (!firstCharacterToggle)
                {
                    characterSlide.GetComponent<Button>().Select();
                    battleController.setSelectedCharacter(character);
                    firstCharacterToggle = true;
                }
            }
            SetupCharacterSlideButtons(true);
        }

        private void SetupCharacterSlideButtons(bool enable)
        {
            if (characterSlides == null) { return; }
            foreach (CharacterSlide characterSlide in characterSlides)
            {
                characterSlide.GetComponent<Button>().onClick.RemoveAllListeners();
                if (enable)
                {
                    characterSlide.GetComponent<Button>().onClick.AddListener(delegate { battleController.setSelectedCharacter(characterSlide.GetCharacter()); });
                }
            }
        }

        private void SetupEnemies(IEnumerable enemies)
        {
            // TODO:  Add support for two different types of presentation -- two-row small format, one-row large format

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
                enemySlide.SetEnemy(enemy);
                enemySlides.Add(enemySlide);
            }
            skillSelection.SetEnemySlides(enemySlides);
        }

        private void SetupEntryMessage(IEnumerable enemies)
        {
            CombatParticipant enemy = enemies.Cast<CombatParticipant>().FirstOrDefault();
            GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, infoChooseParent);

            string entryMessage;
            if (enemies.Cast<CombatParticipant>().Count() > 1)
            {
                entryMessage = string.Format("You have encountered {0} and its cohort.", enemy.GetCombatName());
            }
            else
            {
                entryMessage = string.Format("You have encountered {0}.", enemy.GetCombatName());
            }

            DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
            dialogueBox.AddSimpleText(entryMessage);
            dialogueBox.AddPageBreak();
            dialogueBox.AddSimpleText("What do you want to do?");
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
                exitMessage = "You Won!  Congratulations!";
            }
            else if (battleController.GetBattleOutcome() == BattleOutcome.Lost)
            {
                exitMessage = "You lost.  Whoops!";
            }
            else if (battleController.GetBattleOutcome() == BattleOutcome.Ran)
            {
                exitMessage = "You ran away.";
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
