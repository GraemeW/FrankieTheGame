using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Frankie.Dialogue.UI;
using static Frankie.Combat.BattleController;
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
        CombatParticipant playerCombatParticipant = null; // TODO:  Implement party concept
        BattleController battleController = null;

        // State
        List<EnemySlide> enemySlides = new List<EnemySlide>();

        // Static
        private static string DIALOGUE_CALLBACK_INTRO_COMPLETE = "INTRO_COMPLETE";

        private void Awake()
        {
            playerCombatParticipant = GameObject.FindGameObjectWithTag("Player").GetComponent<CombatParticipant>();
        }

        private void OnEnable()
        {
            battleController = FindObjectOfType<BattleController>();
            battleController.battleStateChanged += Setup;
        }

        private void OnDisable()
        {
            battleController.battleStateChanged -= Setup;
        }

        public void Setup(BattleState state)
        {
            if (state == BattleState.Intro)
            {
                ClearBattleCanvas();
                SetupPlayer();
                SetupEnemies(battleController.GetEnemies());
                SetupEntryMessage(battleController.GetEnemies());
            }
            else if (state == BattleState.PreCombat)
            {
                combatOptions.gameObject.SetActive(true);
            }
            else if (state == BattleState.Combat)
            {
                skillSelection.gameObject.SetActive(true);
                skillSelection.SetupEnemySlideListeners(enemySlides);
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

        private void SetupPlayer()
        {
            // TODO:  Implement party concept, iterate and spawn multiple slides
            GameObject characterObject = Instantiate(characterSlidePrefab, playerPanelParent);
            CharacterSlide characterSlide = characterObject.GetComponent<CharacterSlide>();
            characterSlide.SetCharacter(playerCombatParticipant);  // TODO:  Implement party concept, pull name from party
            characterSlide.GetComponent<Button>().onClick.AddListener(delegate { battleController.SetActivePlayerCharacter(playerCombatParticipant); });

            // First character only:
            characterSlide.GetComponent<Button>().Select();
            battleController.SetActivePlayerCharacter(playerCombatParticipant);

            // end loop iteration
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
        }

        private void SetupEntryMessage(IEnumerable enemies)
        {
            CombatParticipant enemy = enemies.Cast<CombatParticipant>().FirstOrDefault();
            GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, infoChooseParent);

            string entryMessage = "";
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

        public void HandleDialogueCallback(string callbackMessage)
        {
            if (callbackMessage == DIALOGUE_CALLBACK_INTRO_COMPLETE)
            {
                battleController.SetBattleState(BattleState.PreCombat);
            }
        }
    }
}
