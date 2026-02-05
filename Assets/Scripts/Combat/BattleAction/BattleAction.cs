using System;
using System.Collections;
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
        public float GetAPCost() => apCost;
        
        public bool Use(BattleActionData battleActionData, bool useAP, Action finished)
        {
            if (battleActionData.GetSender().IsDead()) { finished.Invoke(); return false; }
            if (effectStrategies == null || (useAP && !battleActionData.GetSender().HasAP(apCost)) || !battleActionData.HasTargets())
            {
                battleActionData.GetSender().SetCooldown(0f);
                finished?.Invoke();
                return false;
            }

            CombatParticipant sender = battleActionData.GetSender();
            IList<BattleEntity> recipients = battleActionData.GetTargets();
            sender.StartCoroutine(EffectSequence(sender, recipients, useAP, finished));

            return true;
        }

        public void SetTargets(TargetingNavigationType targetingNavigationType, BattleActionData battleActionData, IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            if (battleActionData == null) { return; }
            targetingStrategy.SetTargets(targetingNavigationType, battleActionData, activeCharacters, activeEnemies);
        }
        #endregion

        #region PrivateMethods
        private IEnumerator EffectSequence(CombatParticipant sender, IList<BattleEntity> recipients, bool useAP, Action finished)
        {
            foreach (EffectStrategy effectStrategy in effectStrategies)
            {
                if (effectStrategy == null) { continue; }
                yield return effectStrategy.StartEffect(sender, recipients, damageType);
            }
            
            sender.SetCooldown(cooldown);
            if (useAP) { sender.AdjustAP(-apCost); }
            finished?.Invoke();
        }
        #endregion
    }
}
