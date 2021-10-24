using Frankie.Inventory;
using System.Collections;
using System.Collections.Generic;
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


        public void Use(CombatParticipant sender, IEnumerable<CombatParticipant> recipients)
        {
            // TODO:  check if have enough AP to decide if we're doin' this
            // TODO:  any other checks?  recipients sanity check?

            if (effectStrategies == null) { return; }
            foreach(EffectStrategy effectStrategy in effectStrategies)
            {
                effectStrategy.StartEffect(sender, recipients);
            }
            sender.SetCooldown(cooldown);
            sender.AdjustAP(-apCost);
        }

        public IEnumerable<CombatParticipant> GetTargets(bool? traverseForward, IEnumerable<CombatParticipant> currentTargets, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            IEnumerable<CombatParticipant> targets = targetingStrategy.GetTargets(traverseForward, currentTargets, activeCharacters, activeEnemies);

            return targets;
        }
    }
}
