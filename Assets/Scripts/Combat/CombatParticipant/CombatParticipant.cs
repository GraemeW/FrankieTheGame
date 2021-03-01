using UnityEngine;
using Frankie.Utils;
using Frankie.Stats;
using Frankie.Saving;
using System;
using Frankie.Core;
using System.Collections.Generic;

namespace Frankie.Combat
{
    [RequireComponent(typeof(BaseStats))]
    [RequireComponent(typeof(SkillHandler))]
    public class CombatParticipant : MonoBehaviour, ISaveable, IPredicateEvaluator
    {
        // Tunables
        [Header("Behavior, Hookups")]
        [SerializeField] bool friendly = false;
        [SerializeField] Sprite combatSprite = null;

        [Header("Combat Properties")]
        [SerializeField] float damageTimeSpan = 4.0f;
        [SerializeField] float fractionOfHPInstantOnRevival = 0.5f;

        // Cached References
        BaseStats baseStats = null;

        // State
        bool inCombat = false;
        bool inCooldown = false;
        float cooldownTimer = 0f;
        public float targetHP = 1f;
        float deltaHPTimeFraction = 0.0f;
        LazyValue<bool> isDead;
        LazyValue<float> currentHP;
        LazyValue<float> currentAP;

        // Events
        public event Action<bool> enterCombat;
        public event Action<CombatParticipant, StateAlteredData> stateAltered;
        public event Action<CombatParticipant, int, List<Tuple<string, int>>> characterLevelUp;

        // Static
        static string[] PREDICATES_ARRAY = { "IsAnyoneDead", "IsAnyoneAlive", "IsCharacterDead"};

        private void Awake()
        {
            baseStats = GetComponent<BaseStats>();
            currentHP = new LazyValue<float>(GetMaxHP);
            currentAP = new LazyValue<float>(GetMaxAP);
            isDead = new LazyValue<bool>(SpawnAlive);
        }

        private void OnEnable()
        {
            baseStats.onLevelUp += PassLevelUpMessage;
        }

        private void OnDisable()
        {
            baseStats.onLevelUp -= PassLevelUpMessage;
        }

        private void Start()
        {
            currentHP.ForceInit();
            currentAP.ForceInit();
            isDead.ForceInit();
            targetHP = currentHP.value;
        }

        private void Update()
        {
            if (!inCombat) { return; }
            if (CheckIfDead()) { return; }
            UpdateCooldown();
        }

        private void FixedUpdate()
        {
            UpdateDamageDelayedHealth();
        }

        public void SetCombatActive(bool enable)
        {
            inCombat = enable;
            if (!enable){ HaltHPScroll(); }
            if (enterCombat != null)
            {
                enterCombat.Invoke(enable);
            }
        }

        public float GetHPValueForSkill(Skill skill)
        {
            // TODO:  Implement buff/debuff logic for adjusting this parameter

            if (Mathf.Approximately(skill.hpValue, 0f)) { return 0f; }
            float baseStatsModifier = Mathf.Sign(skill.hpValue) * GetBaseStatsModifier(skill);
            return baseStatsModifier + skill.hpValue;
        }

        public float GetAPValueForSkill(Skill skill)
        {
            // TODO:  Implement buff/debuff logic for adjusting this parameter

            if (Mathf.Approximately(skill.apValue, 0f)) { return 0f; }
            float baseStatsModifier = Mathf.Sign(skill.hpValue) * GetBaseStatsModifier(skill);
            return baseStatsModifier + skill.apValue;
        }

        public float GetCooldownForSkill(Skill skill)
        {
            // TODO:  Implement buff/debuff logic for adjusting this parameter
            return skill.cooldown;
        }

        public bool IsInCombat()
        {
            return inCombat;
        }

        public void AdjustHP(float points)
        {
            if (isDead.value) { return; }

            if (friendly) // Damage dealt is delayed, occurs over damageTimeSpan seconds
            {
                float unsafeHP = currentHP.value + points;
                targetHP = Mathf.Min(unsafeHP, baseStats.GetStat(Stat.HP));
                deltaHPTimeFraction = (Time.deltaTime / damageTimeSpan);
            }
            else
            {
                float unsafeHP = currentHP.value + points;
                currentHP.value = Mathf.Clamp(unsafeHP, 0f, baseStats.GetStat(Stat.HP));
            }

            if (stateAltered != null)
            {
                if (points < 0) { stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.DecreaseHP, points)); }
                else if (points > 0) { stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.IncreaseHP, points)); }
            }
        }

        public void HaltHPScroll()
        {
            if (targetHP > currentHP.value) { return; } // Allow healing to occur post-battle
            deltaHPTimeFraction = 0f;
            targetHP = currentHP.value;
        }

        public void AdjustAP(float points)
        {
            if (isDead.value) { return; }

            float unsafeAP = currentAP.value + points;
            currentAP.value = Mathf.Clamp(unsafeAP, 0f, baseStats.GetStat(Stat.AP));

            if (stateAltered != null)
            {
                if (points < 0) { stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.DecreaseAP, points)); }
                else if (points > 0) { stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.IncreaseAP, points)); }
            }
        }

        public void ApplyStatusEffect(StatusEffect statusEffect)
        {
            ActiveStatusEffect activeStatusEffect = gameObject.AddComponent(typeof(ActiveStatusEffect)) as ActiveStatusEffect;
            activeStatusEffect.Setup(statusEffect);

            if (stateAltered != null)
            {
                stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.StatusEffectApplied, statusEffect.statusEffectType));
            }
        }

        public void ResurrectCharacter(float hp)
        {
            isDead.value = false;
            currentHP.value = hp * fractionOfHPInstantOnRevival;
            targetHP = hp;
            cooldownTimer = 0f;
            if (stateAltered != null)
            {
                stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.Resurrected));
            }
        }

        public void SetCooldown(float seconds)
        {
            inCooldown = true;
            cooldownTimer = seconds;
            if (stateAltered != null)
            {
                stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.CooldownSet));
            }
        }

        public bool GetFriendly()
        {
            return friendly;
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
            return baseStats.GetCharacterProperties().GetCharacterNamePretty();
        }

        public bool IsDead()
        {
            return isDead.value;
        }

        public bool IsInCooldown()
        {
            return inCooldown;
        }

        public void SetMaxHP()
        {
            currentHP.value = baseStats.GetStat(Stat.HP);
        }

        public float GetMaxHP()
        {
            return baseStats.GetStat(Stat.HP);
        }

        public float GetMaxAP()
        {
            return baseStats.GetStat(Stat.AP);
        }

        public float GetExperienceReward()
        {
            return baseStats.GetStat(Stat.ExperienceReward);
        }

        public int GetLevel()
        {
            return baseStats.GetLevel();
        }

        public BaseStats GetBaseStats()
        {
            return baseStats;
        }

        private bool SpawnAlive()
        {
            return false;
        }

        // Private functions
        private bool CheckIfDead()
        {
            if (Mathf.Approximately(currentHP.value, 0f) || currentHP.value < 0)
            {
                currentHP.value = 0f;
                targetHP = 0f;
                isDead.value = true;

                if (stateAltered != null)
                {
                    stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.Dead));
                }
            }
            if (isDead.value) { return true; }
            return false;
        }

        private void UpdateDamageDelayedHealth()
        {
            if (friendly && !Mathf.Approximately(currentHP.value, targetHP))
            {
                deltaHPTimeFraction += (Time.deltaTime / damageTimeSpan);
                float unsafeHP = Mathf.Lerp(currentHP.value, targetHP, deltaHPTimeFraction);
                currentHP.value = Mathf.Clamp(unsafeHP, 0f, baseStats.GetStat(Stat.HP));

                if (stateAltered != null)
                {
                     stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.AdjustHPNonSpecific));
                }
            }
        }

        private void UpdateCooldown()
        {
            if (cooldownTimer > 0) { cooldownTimer -= Time.deltaTime; }
            if (inCooldown && cooldownTimer <= 0)
            {
                inCooldown = false;
                if (stateAltered != null)
                {
                    stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.CooldownExpired));
                }
            }
        }

        private float GetBaseStatsModifier(Skill skill)
        {
            float baseStatsModifier = 0f;
            if (skill.stat != SkillStat.None)
            {
                Stat stat = (Stat)Enum.Parse(typeof(Stat), skill.stat.ToString()); // Enum-to-enum match; SkillStat is a subset of Stat
                baseStatsModifier = baseStats.GetStat(stat);
            }

            return baseStatsModifier;
        }

        private void PassLevelUpMessage(int level, Dictionary<Stat, float> levelUpSheet)
        {
            List<Tuple<string, int>> statNameValuePairs = new List<Tuple<string, int>>();
            foreach (KeyValuePair<Stat, float> entry in levelUpSheet)
            {
                if (entry.Key == Stat.HP) { AdjustHP(entry.Value); }
                if (entry.Key == Stat.AP) { AdjustAP(entry.Value); }

                Tuple<string, int> statNameValuePair = new Tuple<string, int>(entry.Key.ToString(), Mathf.RoundToInt(entry.Value));
                statNameValuePairs.Add(statNameValuePair);
            }

            characterLevelUp.Invoke(this, level, statNameValuePairs);
        }

        // Save State
        [System.Serializable]
        struct CombatParticipantSaveData
        {
            public bool isDead;
            public float currentHP;
            public float currentAP;
        }

        public object CaptureState()
        {
            CombatParticipantSaveData combatParticipantSaveData = new CombatParticipantSaveData
            {
                isDead = isDead.value,
                currentHP = currentHP.value,
                currentAP = currentAP.value
            };
            return combatParticipantSaveData;
        }

        public void RestoreState(object state)
        {
            CombatParticipantSaveData data = (CombatParticipantSaveData)state;
            isDead.value = data.isDead;
            currentHP.value = data.currentHP;
            currentAP.value = data.currentAP;
        }

        // Predicate Evaluation
        public bool? Evaluate(string predicate, string[] parameters)
        {
            string matchingPredicate = this.MatchToPredicates(predicate, PREDICATES_ARRAY);
            if (string.IsNullOrWhiteSpace(matchingPredicate)) { return null; }

            if (predicate == PREDICATES_ARRAY[0])
            {
                return PredicateEvaluateIsAnyoneDead(parameters);
            }
            else if (predicate == PREDICATES_ARRAY[1])
            {
                return PredicateEvaluateIsAnyoneAlive(parameters);
            }
            else if (predicate == PREDICATES_ARRAY[2])
            {
                return PredicateEvaluateIsCharacterDead(parameters);
            }
            return null;
        }

        string IPredicateEvaluator.MatchToPredicatesTemplate()
        {
            // Not evaluated -> PredicateEvaluatorExtension
            return null;
        }

        private bool? PredicateEvaluateIsAnyoneDead(string[] parameters)
        {
            if (IsDead())
            {
                return true;
            }
            return null;
        }

        private bool? PredicateEvaluateIsAnyoneAlive(string[] parameters)
        {
            if (!IsDead())
            {
                return true;
            }
            return null;
        }

        private bool? PredicateEvaluateIsCharacterDead(string[] parameters)
        {
            foreach (string characterName in parameters)
            {
                if (string.Equals(baseStats.GetCharacterProperties().name, characterName))
                {
                    return IsDead();
                }
            }
            return null;
        }
    }
}
