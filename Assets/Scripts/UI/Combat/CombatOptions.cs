using Frankie.Dialogue.UI;
using UnityEngine;
using UnityEngine.UI;
using static Frankie.Combat.BattleController;

namespace Frankie.Combat.UI
{
    public class CombatOptions : DialogueOptionBox
    {
        // Tunables -- TODO:  Linked up to use via keyboard command
        [SerializeField] Button fightButton = null;
        [SerializeField] Button itemButton = null;
        [SerializeField] Button statsButton = null;
        [SerializeField] Button runButton = null;
        [SerializeField] Button bargainButton = null;

        // Cached References
        BattleController battleController = null;

        protected override void Awake()
        {
            base.Awake();
            battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();
        }

        public void InitiateCombat()
        {
            battleController.SetBattleState(BattleState.Combat);
            gameObject.SetActive(false);
        }

        public void AttemptToRun()
        {
            // TODO:  add logic for running (odds vs. speed, etc.)
            battleController.SetBattleOutcome(BattleOutcome.Ran);
            battleController.SetBattleState(BattleState.Outro);
            gameObject.SetActive(false);
        }
    }
}