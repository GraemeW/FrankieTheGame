using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Combat.UI
{
    public class EnemySlide : BattleSlide
    {
        // Tunables
        [SerializeField] Image image = null;
        [SerializeField] float deathFadeTime = 1.0f;
        [SerializeField] DamageTextSpawner damageTextSpawner = null;

        public override void SetCombatParticipant(CombatParticipant combatParticipant)
        {
            base.SetCombatParticipant(combatParticipant);
            UpdateImage(this.combatParticipant.GetCombatSprite());
        }

        protected override void ParseState(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            if (stateAlteredData.stateAlteredType == StateAlteredType.IncreaseHP 
                || stateAlteredData.stateAlteredType == StateAlteredType.DecreaseHP)
            {
                float points = stateAlteredData.points;
                damageTextSpawner.Spawn(points);
                if (stateAlteredData.stateAlteredType == StateAlteredType.DecreaseHP)
                {
                    ShakeSlide(false);   
                }
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.Dead)
            {
                GetComponent<Button>().enabled = false;
                GetComponent<Image>().CrossFadeAlpha(0f, deathFadeTime, false);
            }
            else if (stateAlteredData.stateAlteredType == StateAlteredType.Resurrected)
            {
                GetComponent<Button>().enabled = true;
                GetComponent<Image>().CrossFadeAlpha(1f, deathFadeTime, false);
            }
        }

        protected override void SetSelected(CombatParticipantType combatParticipantType, bool enable)
        {
            if (combatParticipantType != CombatParticipantType.Enemy) { return; }
            GetComponent<Shadow>().enabled = enable;
        }

        // Private Functions
        private void UpdateImage(Sprite sprite)
        {
            image.sprite = sprite;
        }
    }
}
