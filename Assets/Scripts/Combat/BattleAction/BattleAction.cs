using Frankie.Core;
using Frankie.Inventory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Battle Action", menuName = "BattleAction/New Battle Action")]
    public class BattleAction : ScriptableObject, IAddressablesCache
    {
        [Header("Scriptable Object Inputs")]
        [SerializeField] TargetingStrategy targetingStrategy = null;
        [SerializeField] EffectStrategy[] effectStrategies = null;
        [Header("Other Inputs")]
        [SerializeField] DamageType damageType = default;
        [SerializeField] float cooldown = 0f;
        [SerializeField] float apCost = 0f;

        // State
        static AsyncOperationHandle<IList<BattleAction>> addressablesLoadHandle;
        static Dictionary<string, BattleAction> battleActionLookupCache;

        #region AddressablesCaching
        public static BattleAction GetBattleActionFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { return null; }
            BuildCacheIfEmpty();

            if (name == null || !battleActionLookupCache.ContainsKey(name)) return null;
            return battleActionLookupCache[name];
        }

        public static void BuildCacheIfEmpty()
        {
            if (battleActionLookupCache == null)
            {
                BuildBattleActionCache();
            }
        }

        private static void BuildBattleActionCache()
        {
            battleActionLookupCache = new Dictionary<string, BattleAction>();
            addressablesLoadHandle = Addressables.LoadAssetsAsync(typeof(BattleAction).Name, (BattleAction battleAction) =>
            {
                if (battleActionLookupCache.ContainsKey(battleAction.name))
                {
                    Debug.LogError(string.Format("Looks like there's a duplicate ID for objects: {0} and {1}", battleActionLookupCache[battleAction.name], battleAction));
                }

                battleActionLookupCache[battleAction.name] = battleAction;
            }
            );
            addressablesLoadHandle.WaitForCompletion();
        }

        public static void ReleaseCache()
        {
            Addressables.Release(addressablesLoadHandle);
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

            if (effectStrategies == null || !battleActionData.GetSender().HasAP(apCost))
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
                    (EffectStrategy childEffectStrategy) => EffectFinished(battleActionData.GetSender(), childEffectStrategy, finished));
            }
            return true;
        }

        public void GetTargets(bool? traverseForward, BattleActionData battleActionData,
            IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            if (battleActionData == null) { return; }

            targetingStrategy.GetTargets(traverseForward, battleActionData, activeCharacters, activeEnemies);
        }
        #endregion

        #region PrivateMethods
        private void EffectFinished(CombatParticipant sender, EffectStrategy effectStrategy, Action finished)
        {
            if (effectStrategy.GetType() == typeof(TriggerResourcesCooldownsEffect))
            {
                sender.SetCooldown(cooldown);
                sender.AdjustAP(-apCost);

                finished?.Invoke();
            }
        }

        private void CheckForTriggerResources()
        {
#if UNITY_EDITOR
            int numberOfTriggerResources = CountTriggerResourcesEffects(effectStrategies);

            if (numberOfTriggerResources == 0)
            {
                Debug.LogError(string.Format("Warning -- You need to add at least one trigger resources effect for the battle action: {0}", name));
            }
            else if (numberOfTriggerResources > 1)
            {
                Debug.LogError(string.Format("Warning -- looks like there's more than one trigger resources for the battle action: {0}.  Remove the extra instances.", name));
            }
#endif
        }

        private int CountTriggerResourcesEffects(EffectStrategy[] effectStrategiesToCheck)
        {
            int triggerResourceCount = 0;
#if UNITY_EDITOR
            foreach (EffectStrategy effectStrategy in effectStrategiesToCheck)
            {
                Type effectStrategyType = effectStrategy.GetType();

                if (effectStrategyType == typeof(DelayCompositeEffect))
                {
                    DelayCompositeEffect delayCompositeEffect = effectStrategy as DelayCompositeEffect;
                    triggerResourceCount += CountTriggerResourcesEffects(delayCompositeEffect.GetEffectStrategies());
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
