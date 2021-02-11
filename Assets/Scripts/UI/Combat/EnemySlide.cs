using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Frankie.Combat.CombatParticipant;

namespace Frankie.Combat.UI
{
    public class EnemySlide : BattleSlide
    {
        // Tunables
        [SerializeField] Image image = null;
        [SerializeField] float deathFadeTime = 1.0f;
        [SerializeField] DamageTextSpawner damageTextSpawner = null;

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        public override void SetCombatParticipant(CombatParticipant combatParticipant)
        {
            base.SetCombatParticipant(combatParticipant);
            UpdateImage(this.combatParticipant.GetCombatSprite());
        }

        protected override void ParseState(CombatParticipant combatParticipant, StateAlteredType stateAlteredType, float points)
        {
            // TODO:  update slide graphics / animation as a function of altered type input
            if (stateAlteredType == StateAlteredType.IncreaseHP || stateAlteredType == StateAlteredType.DecreaseHP)
            {
                damageTextSpawner.Spawn(points);
                if (stateAlteredType == StateAlteredType.DecreaseHP)
                {
                    ShakeSlide(false);   
                }
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

        protected override void SetSelected(bool enable)
        {
            GetComponent<Shadow>().enabled = enable;
        }

        // Private Functions
        private void UpdateImage(Sprite sprite)
        {
            image.sprite = sprite;
        }
    }
}
