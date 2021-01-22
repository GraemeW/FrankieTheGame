using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;
using Frankie.Stats;
using System.Text.RegularExpressions;

namespace Frankie.Combat
{
    [RequireComponent(typeof(BaseStats))]
    public class CombatParticipant : MonoBehaviour
    {
        // Tunables
        [SerializeField] bool friendly = false;
        [SerializeField] float damageTimeSpan = 4.0f;
        [SerializeField] Sprite combatSprite = null;

        // Cached References
        BaseStats baseStats = null;

        // State
        bool isDead = false;
        float targetHP = 1f;
        float deltaHPTimeFraction = 0.0f;
        LazyValue<float> currentHP;
        LazyValue<float> currentAP;

        private void Awake()
        {
            baseStats = GetComponent<BaseStats>();
            currentHP = new LazyValue<float>(GetMaxHP);
            currentAP = new LazyValue<float>(GetMaxAP);
        }

        private void Start()
        {
            currentHP.ForceInit();
            currentAP.ForceInit();
            targetHP = currentHP.value;
        }

        private void OnEnable()
        {
            baseStats.onLevelUp += RestoreHPOnLevelUp;
        }

        private void OnDisable()
        {
            baseStats.onLevelUp -= RestoreHPOnLevelUp;
        }

        private void FixedUpdate()
        {
            UpdateDamageDelayedHealth();
            CheckIfDead();
        }

        public void AdjustHP(float points)
        {
            if (isDead) { return; }

            if (friendly) // Damage dealt is delayed, occurs over damageTimeSpan seconds
            {
                targetHP += points;
                deltaHPTimeFraction = (Time.deltaTime / damageTimeSpan);
            }
            else
            {
                float unsafeHP = currentHP.value + points;
                currentHP.value = Mathf.Clamp(unsafeHP, 0f, baseStats.GetStat(Stat.HP));
            }
            CheckIfDead();
        }

        public void AdjustAP(float points)
        {
            if (isDead) { return; }

            float unsafeAP = currentAP.value + points;
            currentAP.value = Mathf.Clamp(unsafeAP, 0f, baseStats.GetStat(Stat.AP));
        }

        public float GetHP()
        {
            return currentHP.value;
        }

        public float GetAP()
        {
            return currentAP.value;
        }

        public Sprite GetCombatSprite()
        {
            return combatSprite;
        }

        public string GetCombatName()
        {
            // Split apart name on lower case followed by upper case w/ or w/out underscores
            return Regex.Replace(baseStats.GetCharacterName().ToString("G"), "([a-z])_?([A-Z])", "$1 $2");
        }

        public bool IsDead()
        {
            return isDead;
        }

        public void SetMaxHealth()
        {
            currentHP.value = baseStats.GetStat(Stat.HP);
        }

        public void RestoreHPOnLevelUp()
        {
            float maxHP = baseStats.GetStat(Stat.HP);
            float differenceToMaxHP = Mathf.Clamp((maxHP - currentHP.value), 0, maxHP);
            AdjustHP(differenceToMaxHP);
        }

        private float GetMaxHP()
        {
            return baseStats.GetStat(Stat.HP);
        }

        private float GetMaxAP()
        {
            return baseStats.GetStat(Stat.AP);
        }

        private void UpdateDamageDelayedHealth()
        {
            if (friendly && !Mathf.Approximately(currentHP.value, targetHP))
            {
                deltaHPTimeFraction += (Time.deltaTime / damageTimeSpan);
                float unsafeHP = Mathf.Lerp(currentHP.value, targetHP, deltaHPTimeFraction);
                currentHP.value = Mathf.Clamp(unsafeHP, 0f, baseStats.GetStat(Stat.HP));
            }
        }

        private void CheckIfDead()
        {
            if (Mathf.Approximately(currentHP.value, 0f) || currentHP.value < 0)
            {
                currentHP.value = 0f;
                isDead = true;
                if (!friendly)
                {
                    AwardExperience();
                }
            }
        }

        private void AwardExperience()
        {
            // TODO:  Implement experience awards (requires first:  party concept)
        }
    }
}
