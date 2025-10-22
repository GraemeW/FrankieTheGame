using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Frankie.Core;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Battle Action", menuName = "BattleAction/New Battle Action")]
    public class BattleAction : ScriptableObject, IAddressablesCache
    {
        [Header("Scriptable Object Inputs")]
        [SerializeField] private TargetingStrategy targetingStrategy;
        [SerializeField] private EffectStrategy[] effectStrategies;
        [Header("Other Inputs")]
        [SerializeField] private DamageType damageType;
        [SerializeField] private float cooldown;
        [SerializeField] private float apCost;

        // State
        private static AsyncOperationHandle<IList<BattleAction>> _addressablesLoadHandle;
        private static Dictionary<string, BattleAction> _battleActionLookupCache;

        #region AddressablesCaching
        public static BattleAction GetBattleActionFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { return null; }
            BuildCacheIfEmpty();
            return _battleActionLookupCache.GetValueOrDefault(name);
        }

        public static void BuildCacheIfEmpty()
        {
            if (_battleActionLookupCache == null)
            {
                BuildBattleActionCache();
            }
        }

        private static void BuildBattleActionCache()
        {
            _battleActionLookupCache = new Dictionary<string, BattleAction>();
            _addressablesLoadHandle = Addressables.LoadAssetsAsync(nameof(BattleAction), (BattleAction battleAction) =>
            {
                if (_battleActionLookupCache.TryGetValue(battleAction.name, out BattleAction value)) { Debug.LogError($"Looks like there's a duplicate ID for objects: {value} and {battleAction}"); }
                _battleActionLookupCache[battleAction.name] = battleAction;
            }
            );
            _addressablesLoadHandle.WaitForCompletion();
        }

        public static void ReleaseCache()
        {
            Addressables.Release(_addressablesLoadHandle);
        }
        #endregion

        #region PublicMethods
        public float GetAPCost()
        {
            return apCost;
        }

        public bool Use(BattleActionData battleActionData, Action finished)
        {
            CheckForTriggerResources();

            if (battleActionData.GetSender().IsDead()) { finished.Invoke(); return false; }
            if (effectStrategies == null || !battleActionData.GetSender().HasAP(apCost) || !battleActionData.HasTargets())
            {
                battleActionData.GetSender().SetCooldown(0f);
                finished?.Invoke();
                return false;
            }

            // Useful Debug
            //UnityEngine.Debug.Log($"Using battle action: {name}");
            foreach (EffectStrategy effectStrategy in effectStrategies)
            {
                if (effectStrategy == null) { continue; }
                // Useful Debug
                //UnityEngine.Debug.Log($"Applying effect: {effectStrategy.name}");

                effectStrategy.StartEffect(battleActionData.GetSender(), battleActionData.GetTargets(), damageType,
                    childEffectStrategy => EffectFinished(battleActionData.GetSender(), childEffectStrategy, finished));
            }
            return true;
        }

        public void SetTargets(TargetingNavigationType targetingNavigationType, BattleActionData battleActionData,
            IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            if (battleActionData == null) { return; }

            targetingStrategy.SetTargets(targetingNavigationType, battleActionData, activeCharacters, activeEnemies);
        }
        #endregion

        #region PrivateMethods
        private void EffectFinished(CombatParticipant sender, EffectStrategy effectStrategy, Action finished)
        {
            if (effectStrategy.GetType() != typeof(TriggerResourcesCooldownsEffect)) return;
            sender.SetCooldown(cooldown);
            sender.AdjustAP(-apCost);
            finished?.Invoke();
        }

        private void CheckForTriggerResources()
        {
#if UNITY_EDITOR
            int numberOfTriggerResources = CountTriggerResourcesEffects(effectStrategies);
            switch (numberOfTriggerResources)
            {
                case 0:
                    Debug.LogError($"Warning -- You need to add at least one trigger resources effect for the battle action: {name}");
                    break;
                case > 1:
                    Debug.LogError($"Warning -- looks like there's more than one trigger resources for the battle action: {name}.  Remove the extra instances.");
                    break;
            }
#endif
        }

        private static int CountTriggerResourcesEffects(EffectStrategy[] effectStrategiesToCheck)
        {
            int triggerResourceCount = 0;
#if UNITY_EDITOR
            foreach (EffectStrategy effectStrategy in effectStrategiesToCheck)
            {
                Type effectStrategyType = effectStrategy.GetType();

                if (effectStrategyType == typeof(DelayCompositeEffect))
                {
                    var delayCompositeEffect = effectStrategy as DelayCompositeEffect;
                    if (delayCompositeEffect != null)
                    {
                        triggerResourceCount += CountTriggerResourcesEffects(delayCompositeEffect.GetEffectStrategies());
                    }
                }

                if (effectStrategyType == typeof(TriggerResourcesCooldownsEffect))
                {
                    triggerResourceCount++;
                }
            }
#endif
            return triggerResourceCount;
        }
        #endregion
    }
}
