using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Frankie.Dialogue.UI;
using static Frankie.Combat.BattleHandler;
using static Frankie.Combat.CombatParticipant;

namespace Frankie.Combat.UI
{
    public class BattleCanvas : MonoBehaviour, IDialogueBoxCallbackReceiver
    {
        // Tunables
        [SerializeField] Transform playerPanelParent = null;
        [SerializeField] GameObject characterSlidePrefab = null;
        [SerializeField] Transform frontRowParent = null;
        [SerializeField] Transform backRowParent = null;
        [SerializeField] GameObject enemyPrefab = null;
        [SerializeField] Transform infoChooseParent = null;
        [SerializeField] GameObject dialogueBoxPrefab = null;

        // Cached References
        CombatParticipant playerCombatParticipant = null; // TODO:  Implement party concept
        BattleHandler battleHandler = null;

        // State
        Dictionary<CombatParticipant, CharacterSlide> playerLookup = new Dictionary<CombatParticipant, CharacterSlide>();
        Dictionary<CombatParticipant, EnemySlide> enemyLookup = new Dictionary<CombatParticipant, EnemySlide>();

        // Static
        private static string DIALOGUE_CALLBACK_INTRO_COMPLETE = "INTRO_COMPLETE";

        private void Awake()
        {
            playerCombatParticipant = GameObject.FindGameObjectWithTag("Player").GetComponent<CombatParticipant>();
        }

        private void OnEnable()
        {
            battleHandler = FindObjectOfType<BattleHandler>();
            battleHandler.battleStateChanged += Setup;
        }

        private void OnDisable()
        {
            battleHandler.battleStateChanged -= Setup;
        }

        public void Setup(BattleState state)
        {
            if (state == BattleState.Intro)
            {
                ClearBattleCanvas();
                SetupPlayer();
                SetupEnemies(battleHandler.GetEnemies());
                SetupEntryMessage(battleHandler.GetEnemies());
            }
            else if (state == BattleState.PreCombat)
            {

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
                Destroy(child.gameObject);
            }
        }

        private void SetupPlayer()
        {
            // TODO:  Implement party concept, iterate and spawn multiple slides
            GameObject characterObject = Instantiate(characterSlidePrefab, playerPanelParent);
            CharacterSlide characterSlide = characterObject.GetComponent<CharacterSlide>();
            characterSlide.UpdateName("Frankie"); // TODO:  Implement party concept, pull name from party
            characterSlide.UpdateHP(playerCombatParticipant.GetHP());
            characterSlide.UpdateAP(playerCombatParticipant.GetAP());

            playerLookup.Add(playerCombatParticipant, characterSlide);
            playerCombatParticipant.stateAltered += ProcessPlayerStateChange;
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
                enemySlide.UpdateImage(enemy.GetCombatSprite());

                enemyLookup.Add(enemy, enemySlide);
                enemy.stateAltered += ProcessEnemyStateChange;
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

        private void SetupPreCombatChoices()
        {
            // TODO:  Implement pre-combat choices, then kick off combat loop in same way as intro
        }

        private void ProcessPlayerStateChange(CombatParticipant combatParticipant, StateAlteredType stateAlteredType)
        {

        }

        private void ProcessEnemyStateChange(CombatParticipant combatParticipant, StateAlteredType stateAlteredType)
        {

        }

        public void HandleDialogueCallback(string callbackMessage)
        {
            if (callbackMessage == DIALOGUE_CALLBACK_INTRO_COMPLETE)
            {
                battleHandler.SetBattleState(BattleState.PreCombat);
            }
        }
    }
}
