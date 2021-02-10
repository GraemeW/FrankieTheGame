using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Frankie.Combat.CombatParticipant;

namespace Frankie.Combat.UI
{
    public class EnemySlide : MonoBehaviour
    {
        // Tunables
        [SerializeField] Image image = null;
        [SerializeField] float deathFadeTime = 1.0f;
        [SerializeField] DamageTextSpawner damageTextSpawner = null;

        // State
        CombatParticipant enemy = null;

        // Cached References
        BattleController battleController = null;

        private void Awake()
        {
            battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();
        }

        private void OnEnable()
        {
            battleController.selectedEnemyChanged += HighlightEnemy;
        }

        private void OnDisable()
        {
            battleController.selectedEnemyChanged -= HighlightEnemy;
            if (enemy != null)
            {
                enemy.stateAltered -= ParseEnemyState;
            }
        }

        public void SetEnemy(CombatParticipant combatParticipant)
        {
            enemy = combatParticipant;
            UpdateImage(enemy.GetCombatSprite());
            enemy.stateAltered += ParseEnemyState;
        }

        public CombatParticipant GetEnemy()
        {
            return enemy;
        }

        private void ParseEnemyState(CombatParticipant combatParticipant, StateAlteredType stateAlteredType, float points)
        {
            // TODO:  update slide graphics / animation as a function of altered type input
            if (stateAlteredType == StateAlteredType.IncreaseHP || stateAlteredType == StateAlteredType.DecreaseHP)
            {
                damageTextSpawner.Spawn(points);
            }
            else if (stateAlteredType == StateAlteredType.Dead)
            {
                GetComponent<Button>().enabled = false;
                GetComponent<Image>().CrossFadeAlpha(0f, deathFadeTime, false);
            }
            else if (stateAlteredType == StateAlteredType.Resurrected)
            {
                GetComponent<Button>().enabled = true;
                GetComponent<Image>().CrossFadeAlpha(1f, deathFadeTime, false);
            }
        }

        private void HighlightEnemy(CombatParticipant combatParticipant)
        {
            if (combatParticipant == enemy)
            {
                SetSelected(true);
            }
            else
                SetSelected(false);
        }

        private void SetSelected(bool enable)
        {
            GetComponent<Shadow>().enabled = enable;
        }

        private void UpdateImage(Sprite sprite)
        {
            image.sprite = sprite;
        }
    }
}
