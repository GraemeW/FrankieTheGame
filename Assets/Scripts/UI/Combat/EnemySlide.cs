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

        // State
        CombatParticipant enemy = null;

        private void OnDisable()
        {
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

        private void ParseEnemyState(StateAlteredType stateAlteredType)
        {
            // TODO:  update slide graphics / animation as a function of altered type input
            if (stateAlteredType == StateAlteredType.Dead)
            {
                // TODO:  add graphic here (nice fader or whatever)
                gameObject.SetActive(false);
            }
        }

        private void UpdateImage(Sprite sprite)
        {
            image.sprite = sprite;
        }
    }
}
