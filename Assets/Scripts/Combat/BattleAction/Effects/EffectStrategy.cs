using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    public abstract class EffectStrategy : ScriptableObject
    {
        public abstract void StartEffect(CombatParticipant sender, IEnumerable<BattleEntity> recipients, DamageType damageType, Action<EffectStrategy> finished);

        public static void StartCoroutine(CombatParticipant sender, IEnumerator coroutine)
        {
            sender.GetComponent<MonoBehaviour>().StartCoroutine(coroutine);
        }

        protected bool DoesAttackHit(bool canMiss, CombatParticipant sender, CombatParticipant recipient)
        {
            if (!canMiss) { return true; }
            float hitChance = sender.GetCalculatedStat(CalculatedStat.HitChance, recipient);
            float hitRoll = UnityEngine.Random.Range(0f, 1f);

            // Need to invert for miss -- e.g. hitChance = 0.75, 75% chance to hit
            // hitRoll > hitChance = 25%, or 25% chance to miss -> skip adjust HP, call out the miss on the target
            if (hitRoll > hitChance)
            {
                recipient.AnnounceStateUpdate(new StateAlteredData(StateAlteredType.HitMiss));
                return false;
            }
            return true;
        }

        protected float GetCritModifier(bool canCrit, float critMultiplier, CombatParticipant sender, CombatParticipant recipient)
        {
            if (!canCrit) { return 1.0f; }

            float critChance = sender.GetCalculatedStat(CalculatedStat.CritChance, recipient);
            float critRoll = UnityEngine.Random.Range(0f, 1f);

            if (critRoll <= critChance)
            {
                recipient.AnnounceStateUpdate(new StateAlteredData(StateAlteredType.HitCrit));
                return critMultiplier;
            }
            return 1.0f;
        }

        protected float GetPhysicalModifier(float sign, CombatParticipant sender, CombatParticipant recipient)
        {
            if (sign > 0)
            {
                return sign * Mathf.Max(0f, sender.GetCalculatedStat(CalculatedStat.PhysicalAdder));
            }
            else if (sign < 0)
            {
                return sign * Mathf.Max(0f, sender.GetCalculatedStat(CalculatedStat.PhysicalAdder) - recipient.GetCalculatedStat(CalculatedStat.Defense));
            }
            return 0f;
        }

        protected float GetMagicalModifier(float sign, CombatParticipant sender, CombatParticipant recipient)
        {
            return sign * Mathf.Max(0f, sender.GetCalculatedStat(CalculatedStat.MagicalAdder));
        }
    }
}