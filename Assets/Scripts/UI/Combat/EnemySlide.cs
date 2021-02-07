using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Frankie.Combat.CombatParticipant;
using TMPro; // TEMP

namespace Frankie.Combat.UI
{
    public class EnemySlide : MonoBehaviour
    {
        // Tunables
        [SerializeField] Image image = null;
        [SerializeField] TextMeshProUGUI tempHealthField = null;
        [SerializeField] float deathFadeTime = 1.0f;

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
            TempUpdateHP();
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
                TempUpdateHP();
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

        private void UpdateImage(Sprite sprite)
        {
            image.sprite = sprite;
        }

        private void TempUpdateHP()
        {
            tempHealthField.text = Mathf.RoundToInt(enemy.GetHP()).ToString();
        }
    }
}
