using Frankie.Speech.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Combat.UI
{
    public class CombatOptions : DialogueOptionBox
    {
        // Tunables 
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

        protected override void OnEnable()
        {
            base.OnEnable();
            battleController.globalInput += HandleGlobalInput;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            battleController.globalInput -= HandleGlobalInput;
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