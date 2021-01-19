using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Frankie.Dialogue.UI;

namespace Frankie.Combat.UI
{
    public class BattleCanvas : MonoBehaviour
    {
        // Tunables
        [SerializeField] Transform playerPanelParent = null;
        [SerializeField] GameObject characterSlide = null;
        [SerializeField] Transform frontRowParent = null;
        [SerializeField] Transform backRowParent = null;
        [SerializeField] GameObject enemyPrefab = null;
        [SerializeField] Transform infoChooseParent = null;
        [SerializeField] GameObject dialogueBoxPrefab = null;

        // Cached References
        CombatParticipant playerCombatParticipant = null; // TODO:  Implement party concept
        BattleHandler battleHandler = null;

        // State
        CharacterSlide characterOneSlide = null;
        CharacterSlide characterTwoSlide = null;
        CharacterSlide characterThreeSlide = null;
        CharacterSlide characterFourSlide = null;

        private void Awake()
        {
            playerCombatParticipant = GameObject.FindGameObjectWithTag("Player").GetComponent<CombatParticipant>();
        }

        private void OnEnable()
        {
            battleHandler = FindObjectOfType<BattleHandler>();
            battleHandler.enemiesUpdated += Setup;
        }


        public void Setup()
        {
            if (battleHandler == null) { battleHandler = FindObjectOfType<BattleHandler>(); }
            ClearBattleCanvas();
            SetupPlayer();
            SetupEnemies(battleHandler.GetEnemies());
            SetupEntryMessage(battleHandler.GetEnemies());
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
            GameObject characterOneObject = Instantiate(characterSlide, playerPanelParent);
            characterOneSlide = characterOneObject.GetComponent<CharacterSlide>();
            characterOneSlide.UpdateName("Frankie"); // TODO:  Implement party concept, pull name from party
            characterOneSlide.UpdateHP(playerCombatParticipant.GetHP());
            characterOneSlide.UpdateAP(playerCombatParticipant.GetAP());
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
            }
        }

        private void SetupEntryMessage(IEnumerable enemies)
        {
            CombatParticipant enemy = enemies.Cast<CombatParticipant>().FirstOrDefault();
            GameObject dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);

            string entryMessage = "";
            if (enemies.Cast<CombatParticipant>().Count() > 1)
            {
                entryMessage = string.Format("You have encountered {0} and its cohort.", enemy.GetCombatName());
            }
            else
            {
                entryMessage = string.Format("You have encountered {0}.", enemy.GetCombatName());
            }

            dialogueBox.GetComponent<DialogueBox>().AddSimpleText(entryMessage);
            dialogueBox.GetComponent<DialogueBox>().AddSimpleText("Here is what he says:");
            dialogueBox.GetComponent<DialogueBox>().AddSimpleSpeech("Prepare for your punishment");
        }
    }
}
