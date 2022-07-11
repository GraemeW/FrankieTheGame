using Frankie.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Combat
{
    [RequireComponent(typeof(CombatParticipant))]
    [RequireComponent(typeof(SkillHandler))]
    public class BattleAI : MonoBehaviour, IPredicateEvaluator
    {
        // Tunables
        [SerializeField][Range(0, 1)] float probabilityToTraverseSkillTree = 0.8f;
        [SerializeField] bool useRandomSelectionOnNoPriorities = true;
        [SerializeField] BattleAIPriority[] battleAIPriorities = null;

        // State
        List<Skill> skillsToExclude = new List<Skill>();
        List<CombatParticipant> localAllies = new List<CombatParticipant>();
        List<CombatParticipant> localFoes = new List<CombatParticipant>();


        // Cached References
        CombatParticipant combatParticipant = null;
        SkillHandler skillHandler = null;
        BattleController battleController = null;

        #region UnityMethods
        private void Awake()
        {
            combatParticipant = GetComponent<CombatParticipant>();
            skillHandler = GetComponent<SkillHandler>();
        }

        private void OnEnable()
        {
            combatParticipant.enterCombat += UpdateBattleController;
        }

        private void OnDisable()
        {
            combatParticipant.enterCombat -= UpdateBattleController;
        }

        private void Update()
        {
            if (battleController == null) { return; }
            QueueNextAction();
        }
        #endregion

        #region PublicMethods
        public List<CombatParticipant> GetLocalAllies() => localAllies;
        public List<CombatParticipant> GetLocalFoes() => localFoes;
        #endregion

        #region PrivateMethods
        private void UpdateBattleController(bool active)
        {
            if (active)
            { 
                battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();
                skillsToExclude.Clear();
            }
            else 
            { 
                battleController = null; 
            }
        }

        private void QueueNextAction()
        {
            if (battleController.GetBattleState() == BattleState.Combat 
                && !combatParticipant.IsDead() && combatParticipant.IsInCombat() && !combatParticipant.IsInCooldown())
            {
                bool isFriendly = combatParticipant.GetFriendly();
                // Define local lists of characters -- required to copy since shuffling (if pertinent) done in place
                // Note:  Ally/foe refers to that mob's disposition specifically, i.e. as a function of if it's friendly to characters
                localAllies = isFriendly ? new List<CombatParticipant>(battleController.GetCharacters()) : new List<CombatParticipant>(battleController.GetEnemies());
                localFoes = isFriendly ? new List<CombatParticipant>(battleController.GetEnemies()) : new List<CombatParticipant>(battleController.GetCharacters()); 

                Skill skill = GetSkill(out BattleAIPriority battleAIPriority, localAllies, localFoes);
                if (skill == null) { return; }
                if (battleAIPriority == null && !useRandomSelectionOnNoPriorities) { return; } // Edge case, should be caught by above -- do nothing if no smarter AIs available

                BattleActionData battleActionData = new BattleActionData(combatParticipant);
                if (battleAIPriority != null)
                {
                    battleAIPriority.SetTarget(this, battleActionData, skill);
                }
                else
                {
                    BattleAIPriority.SetRandomTarget(this, battleActionData, skill);
                }

                // If targetCount is 0, skill isn't possible -- prohibit skill from selection & restart
                if (battleActionData.targetCount == 0)
                {
                    skillsToExclude.Add(skill);
                    QueueNextAction();
                }
                else
                {
                    // Useful Debug
                    //string targetNames = string.Concat(battleActionData.GetTargets().Select(x => x.name));
                    //UnityEngine.Debug.Log($"{battleActionData.GetSender().name} adding action {skill.name} to queue for targets {targetNames}");
                    battleController.AddToBattleQueue(battleActionData, skill);
                    ClearSelectionMemory();
                }
            }
        }

        private Skill GetSkill(out BattleAIPriority chosenBattleAIPriority, List<CombatParticipant> localAllies, List<CombatParticipant> localFoes)
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
                // Default behavior -- choose at random, no battle AI priority selected
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
            BattleAIPredicate battleAIPredicate = predicate as BattleAIPredicate;
            return battleAIPredicate != null ? battleAIPredicate.Evaluate(this) : null;
        }
        #endregion
    }
}
