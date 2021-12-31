using Frankie.Inventory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Battle Action", menuName = "BattleAction/New Battle Action")]
    public class BattleAction : ScriptableObject
    {
        [Header("Scriptable Object Inputs")]
        [SerializeField] TargetingStrategy targetingStrategy = null;
        [SerializeField] EffectStrategy[] effectStrategies = null;
        [Header("Other Inputs")]
        [SerializeField] float cooldown = 0f;
        [SerializeField] float apCost = 0f;

        // State
        static Dictionary<string, BattleAction> battleActionLookupCache;

        public static BattleAction GetBattleActionFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { return null; }

            if (battleActionLookupCache == null)
            {
                BuildBattleActionCache();
            }
            if (name == null || !battleActionLookupCache.ContainsKey(name)) return null;
            return battleActionLookupCache[name];
        }

        private static void BuildBattleActionCache()
        {
            battleActionLookupCache = new Dictionary<string, BattleAction>();
            BattleAction[] battleActionList = Resources.LoadAll<BattleAction>("");
            foreach (BattleAction battleAction in battleActionList)
            {
                if (battleActionLookupCache.ContainsKey(battleAction.name))
                {
                    Debug.LogError(string.Format("Looks like there's a duplicate ID for objects: {0} and {1}", battleActionLookupCache[battleAction.name], battleAction));
                    continue;
                }

                battleActionLookupCache[battleAction.name] = battleAction;
            }
        }

        public float GetAPCost()
        {
            return apCost;
        }

        public bool Use(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, Action finished)
        {
            if (effectStrategies == null || !sender.HasAP(apCost))
            {
                sender.SetCooldown(0f);
                finished?.Invoke();
                return false;
            }

            foreach (EffectStrategy effectStrategy in effectStrategies)
            {
                effectStrategy.StartEffect(sender, recipients,
                    (EffectStrategy childEffectStrategy) => EffectFinished(sender, childEffectStrategy, finished));
            }
            return true;
        }

        private void EffectFinished(CombatParticipant sender, EffectStrategy effectStrategy, Action finished)
        {
            if (effectStrategy.GetType() == typeof(TriggerResourcesCooldownsEffect))
            {
                sender.SetCooldown(cooldown);
                sender.AdjustAP(-apCost);

                finished?.Invoke();
            }
        }

        public IEnumerable<CombatParticipant> GetTargets(bool? traverseForward, IEnumerable<CombatParticipant> currentTargets, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            IEnumerable<CombatParticipant> targets = targetingStrategy.GetTargets(traverseForward, currentTargets, activeCharacters, activeEnemies);

            return targets;
        }
    }
}
