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
            switch (stateAlteredData.stateAlteredType)
            {
                case StateAlteredType.CooldownSet:
                case StateAlteredType.CooldownExpired:
                    break;
                case StateAlteredType.AdjustHPNonSpecific:
                case StateAlteredType.IncreaseHP:
                case StateAlteredType.DecreaseHP:
                    float points = stateAlteredData.points;
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HealthChanged, points));
                    if (stateAlteredData.stateAlteredType == StateAlteredType.DecreaseHP)
                    {
                        ShakeSlide(false);
                    }
                    break;
                case StateAlteredType.IncreaseAP:
                case StateAlteredType.DecreaseAP:
                    break;
                case StateAlteredType.HitMiss:
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HitMiss));
                    break;
                case StateAlteredType.HitCrit:
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HitCrit));
                    break;
                case StateAlteredType.StatusEffectApplied:
                case StateAlteredType.BaseStateEffectApplied:
                    break;
                case StateAlteredType.Dead:
                    button.enabled = false;
                    GetComponent<Image>().CrossFadeAlpha(0f, deathFadeTime, false);
                    break;
                case StateAlteredType.Resurrected:
                    button.enabled = true;
                    GetComponent<Image>().CrossFadeAlpha(1f, deathFadeTime, false);
                    break;
            }
        }

        protected override void SetSelected(CombatParticipantType combatParticipantType, bool enable)
        {
            if (combatParticipantType != CombatParticipantType.Target) { return; }
            GetComponent<Shadow>().enabled = enable;
        }

        // Private Functions
        private void UpdateImage(Sprite sprite)
        {
            image.sprite = sprite;
        }
    }
}