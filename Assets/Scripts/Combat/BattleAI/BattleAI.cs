using System.Collections.Generic;
using UnityEngine;
using Frankie.Core;

namespace Frankie.Combat
{
    [RequireComponent(typeof(CombatParticipant))]
    [RequireComponent(typeof(SkillHandler))]
    public class BattleAI : MonoBehaviour, IPredicateEvaluator
    {
        // Note:  Ally/foe refers to that mob's disposition specifically -- i.e. as a function of if it's friendly to characters
        
        // Tunables
        [SerializeField] private float actionQueuePollingPeriod = 0.1f;
        [SerializeField][Range(0, 1)] private float probabilityToTraverseSkillTree = 0.8f;
        [SerializeField] private bool useRandomSelectionOnNoPriorities = true;
        [SerializeField] private BattleAIPriority[] battleAIPriorities;

        // State
        private bool inActiveCombat = false;
        private float pollingTime = 0f;
        private readonly List<Skill> skillsToExclude = new();
        private List<BattleEntity> localAllies = new();
        private List<BattleEntity> localFoes = new();

        // Cached References
        private CombatParticipant combatParticipant;
        private SkillHandler skillHandler;
        private IList<BattleEntity> cachedAllies;
        private IList<BattleEntity> cachedFoes;

        #region UnityMethods
        private void Awake()
        {
            combatParticipant = GetComponent<CombatParticipant>();
            skillHandler = GetComponent<SkillHandler>();
        }

        private void OnEnable()
        {
            combatParticipant.enteredBattle += SubscribeToBattle;
        }

        private void OnDisable()
        {
            combatParticipant.enteredBattle -= SubscribeToBattle;
        }

        private void Update()
        {
            if (!inActiveCombat) { return; }
            pollingTime += Time.deltaTime;
            if (pollingTime >= actionQueuePollingPeriod)
            {
                // Note:  We could listen for CooldownExpired event, but this can be brittle
                // It's relatively inexpensive to just poll for availability given enemy count
                QueueNextAction();
                pollingTime = 0f;
            }
        }
        #endregion

        #region PublicMethods
        public List<BattleEntity> GetLocalAllies() => localAllies;
        public List<BattleEntity> GetLocalFoes() => localFoes;
        #endregion

        #region PrivateMethods
        private void SubscribeToBattle() { SubscribeToBattle(true); }
        private void SubscribeToBattle(bool enable)
        {
            if (enable)
            {
                BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(HandleBattleStateChangedEvent);
            }
            else
            {
                BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(HandleBattleStateChangedEvent);
            }
        }

        private void HandleBattleStateChangedEvent(BattleStateChangedEvent battleStateChangedEvent)
        {
            inActiveCombat = false;
            switch (battleStateChangedEvent.battleState)
            {
                case BattleState.Combat:
                    cachedAllies = combatParticipant.GetFriendly() ? battleStateChangedEvent.characters : battleStateChangedEvent.enemies;
                    cachedFoes = combatParticipant.GetFriendly() ? battleStateChangedEvent.enemies : battleStateChangedEvent.characters;
                    skillsToExclude.Clear();
                    inActiveCombat = true;
                    break;
                case BattleState.Outro:
                    SubscribeToBattle(false);
                    break;
            }
        }

        private void QueueNextAction()
        {
            if (!BattleController.IsCombatParticipantAvailableToAct(combatParticipant)) { return; }
            
            // Define local lists of characters -- required to copy since shuffling (if pertinent) done in place
            localAllies = new List<BattleEntity>(cachedAllies);
            localFoes = new List<BattleEntity>(cachedFoes);
                
            Skill skill = GetSkill(out BattleAIPriority battleAIPriority);
            if (skill == null) { return; }
            if (battleAIPriority == null && !useRandomSelectionOnNoPriorities) { return; } // Edge case, should be caught by above -- do nothing if no smarter AIs available

            var battleActionData = new BattleActionData(combatParticipant);
            if (battleAIPriority != null)
            {
                battleAIPriority.SetTarget(this, battleActionData, skill);
            }
            else
            {
                BattleAIPriority.SetRandomTarget(this, battleActionData, skill);
            }
            
            // If targetCount is 0, skill isn't possible -- prohibit skill from selection & restart
            if (!battleActionData.HasTargets())
            {
                skillsToExclude.Add(skill);
                QueueNextAction();
            }
            else
            {
                var battleSequence = new BattleSequence(skill, battleActionData);
                BattleEventBus<BattleQueueUpdatedEvent>.Raise(new BattleQueueUpdatedEvent(battleSequence));
                ClearSelectionMemory();
            }
        }

        private Skill GetSkill(out BattleAIPriority chosenBattleAIPriority)
        {
            Skill skill = null;
            if (battleAIPriorities != null)
            {
                foreach (BattleAIPriority battleAIPriority in battleAIPriorities)
                {
                    skill = battleAIPriority.GetSkill(this, skillHandler, skillsToExclude);
                    chosenBattleAIPriority = battleAIPriority;
                    if (skill != null) { return skill; }
                }
            }

            if (useRandomSelectionOnNoPriorities)
            {
                // Default behaviour -- choose at random, no battle AI priority selected
                if (skill == null) { skill = BattleAIPriority.GetRandomSkill(skillHandler, skillsToExclude, probabilityToTraverseSkillTree); }
            }
            chosenBattleAIPriority = null;

            return skill;
        }

        private void ClearSelectionMemory()
        {
            skillHandler.ResetCurrentBranch();
            localAllies.Clear();
            localFoes.Clear();
        }

        public bool? Evaluate(Predicate predicate)
        {
            var battleAIPredicate = predicate as BattleAIPredicate;
            return battleAIPredicate != null ? battleAIPredicate.Evaluate(this) : null;
        }
        #endregion
    }
}
