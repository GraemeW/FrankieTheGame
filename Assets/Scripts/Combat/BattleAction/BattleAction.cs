using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Core;
using LowDefMustard.Utils;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Battle Action", menuName = "Combat/BattleActions/New Battle Action", order = 4)]
    public partial class BattleAction : ScriptableObject, IAddressablesCache
    {
        [Header("Scriptable Object Inputs")]
        [SerializeField] private TargetingStrategy targetingStrategy;
        [SerializeField] private EffectStrategy[] effectStrategies;
        [Header("Other Inputs")]
        [SerializeField] private DamageType damageType;
        [SerializeField] private float cooldown;
        [SerializeField] private float apCost;

        // Editor Field References
        public const string targetingStrategyField = nameof(targetingStrategy);
        public const string effectStrategiesField = nameof(effectStrategies);
        internal const string apCostField = nameof(apCost);

        // State
        [AutoStaticsCleanup] private static AddressablesLookupCache<BattleAction> _cache = new(LDMLogCategory.Combat, nameof(BattleAction), battleAction => battleAction.name);

        #region AddressablesCaching
        public static IAddressablesCacheLifecycle addressablesCache => _cache;
        public static BattleAction GetBattleActionFromName(string name) => _cache.Get(name);
        #endregion

        #region PublicMethods
        public float GetAPCost() => apCost;
        
        public bool Use(BattleActionData battleActionData, bool useAP, Action finished)
        {
            CombatParticipant sender = battleActionData.GetSender();
            if (sender == null || sender.IsDead())
            {
                finished?.Invoke(); 
                return false;
            }

            if (effectStrategies == null || (useAP && !sender.HasAP(apCost)) || !battleActionData.HasTargets())
            {
                sender.SetCooldown(0f);
                finished?.Invoke();
                return false;
            }
            
            IList<BattleEntity> recipients = battleActionData.GetTargets();
            return sender.StartBattleActionCoroutine(EffectSequence(sender, recipients, useAP), finished);
        }

        public void SetTargets(TargetingNavigationType targetingNavigationType, BattleActionData battleActionData, IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            if (battleActionData == null) { return; }
            targetingStrategy.SetTargets(targetingNavigationType, battleActionData, activeCharacters, activeEnemies);
        }
        #endregion

        #region PrivateMethods
        private IEnumerator EffectSequence(CombatParticipant sender, IList<BattleEntity> recipients, bool useAP)
        {
            foreach (EffectStrategy effectStrategy in effectStrategies)
            {
                if (effectStrategy == null) { continue; }
                yield return effectStrategy.StartEffect(sender, recipients, damageType);
            }
            
            sender.SetCooldown(cooldown);
            if (useAP) { sender.AdjustAP(-apCost); }
        }
        #endregion
    }
}
