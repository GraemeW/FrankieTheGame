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

        protected override void Start()
        {
            SetGlobalCallbacks(battleController); // input handled via player controller, immediate override
        }

        public void InitiateCombat() // Called via unity events
        {
            battleController.SetBattleState(BattleState.Combat);
            gameObject.SetActive(false);
        }

        public void AttemptToRun() // Called via unity events
        {
            // TODO:  add logic for running (odds vs. speed, etc.) -- should be calculated on party / combat participants
            battleController.SetBattleOutcome(BattleOutcome.Ran);
            battleController.SetBattleState(BattleState.Outro);
            gameObject.SetActive(false);
        }
    }
}