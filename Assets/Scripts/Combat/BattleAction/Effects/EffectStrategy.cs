using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    public abstract class EffectStrategy : ScriptableObject
    {
        [SerializeField] protected string effectGUID;

        private void OnValidate()
        {
            effectGUID = System.Guid.NewGuid().ToString();
        }
        
        public abstract IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType);

        protected static bool DoesAttackHit(bool canMiss, CombatParticipant sender, CombatParticipant recipient)
        {
            if (!canMiss) { return true; }
            float hitChance = sender.GetCalculatedStat(CalculatedStat.HitChance, recipient);
            float hitRoll = UnityEngine.Random.Range(0f, 1f);

            // Need to invert for miss -- e.g. hitChance = 0.75, 75% chance to hit
            // hitRoll > hitChance = 25%, or 25% chance to miss -> skip adjust HP, call out the miss on the target
            if (hitRoll > hitChance)
            {
                recipient.AnnounceStateUpdate(StateAlteredType.HitMiss);
                return false;
            }
            return true;
        }

        protected static float GetCritModifier(bool canCrit, float critMultiplier, CombatParticipant sender, CombatParticipant recipient)
        {
            if (!canCrit) { return 1.0f; }

            float critChance = sender.GetCalculatedStat(CalculatedStat.CritChance, recipient);
            float critRoll = UnityEngine.Random.Range(0f, 1f);

            if (critRoll <= critChance)
            {
                recipient.AnnounceStateUpdate(StateAlteredType.HitCrit);
                return critMultiplier;
            }
            return 1.0f;
        }

        protected static float GetPhysicalModifier(float sign, CombatParticipant sender, CombatParticipant recipient)
        {
            return sign switch
            {
                > 0 => sign * Mathf.Max(0f, sender.GetCalculatedStat(CalculatedStat.PhysicalAdder)),
                < 0 => sign * Mathf.Max(0f,
                    sender.GetCalculatedStat(CalculatedStat.PhysicalAdder) -
                    recipient.GetCalculatedStat(CalculatedStat.Defense)),
                _ => 0f
            };
        }

        protected static float GetMagicalModifier(float sign, CombatParticipant sender, CombatParticipant recipient)
        {
            return sign * Mathf.Max(0f, sender.GetCalculatedStat(CalculatedStat.MagicalAdder));
        }
    }
}
