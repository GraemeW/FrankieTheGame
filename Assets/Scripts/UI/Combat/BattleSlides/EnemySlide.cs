using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Combat.UI
{
    public class EnemySlide : BattleSlide
    {
        // Tunables
        [Header("Enemy Slide Settings")]
        [SerializeField] Image image = null;
        [SerializeField] float deathFadeTime = 1.0f;

        public override void SetBattleEntity(BattleEntity battleEntity)
        {
            base.SetBattleEntity(battleEntity);
            UpdateImage(this.battleEntity.combatParticipant.GetCombatSprite());
        }

        protected override void ParseState(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            switch (stateAlteredData.stateAlteredType)
            {
                case StateAlteredType.CooldownSet:
                    cooldownTimer.ResetTimer(stateAlteredData.points);
                    break;
                case StateAlteredType.CooldownExpired:
                    cooldownTimer.ResetTimer(0f);
                    break;
                case StateAlteredType.AdjustHPNonSpecific:
                case StateAlteredType.IncreaseHP:
                case StateAlteredType.DecreaseHP:
                    float points = stateAlteredData.points;
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HealthChanged, points));
                    if (stateAlteredData.stateAlteredType == StateAlteredType.DecreaseHP)
                    {
                        ShakeSlide(false);
                        BlipFadeSlide();
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
                    image.CrossFadeAlpha(0f, deathFadeTime, false);
                    cooldownTimer?.gameObject.SetActive(false);
                    break;
                case StateAlteredType.Resurrected:
                    button.enabled = true;
                    image.CrossFadeAlpha(1f, deathFadeTime, false);
                    cooldownTimer?.gameObject.SetActive(true);
                    break;
            }
        }

        protected override void SetSelected(CombatParticipantType combatParticipantType, bool enable)
        {
            if (combatParticipantType != CombatParticipantType.Foe) { return; }
            GetComponent<Shadow>().enabled = enable;
        }

        // Private Functions
        private void UpdateImage(Sprite sprite)
        {
            image.sprite = sprite;
        }
    }
}
