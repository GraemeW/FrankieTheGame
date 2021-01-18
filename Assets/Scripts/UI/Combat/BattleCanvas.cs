using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            SetupPlayer();
            SetupEnemies(battleHandler.GetEnemies());
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
    }
}
