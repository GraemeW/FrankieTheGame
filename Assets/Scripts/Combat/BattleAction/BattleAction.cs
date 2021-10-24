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
