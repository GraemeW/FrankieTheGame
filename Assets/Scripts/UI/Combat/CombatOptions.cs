using Frankie.Speech.UI;
using Frankie.Stats.UI;
using Frankie.Stats;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Combat.UI
{
    public class CombatOptions : DialogueOptionBox
    {
        // Tunables 
        [Header("Button HookUps")]
        [SerializeField] Button fightButton = null;
        [SerializeField] Button itemButton = null;
        [SerializeField] Button statsButton = null;
        [SerializeField] Button runButton = null;
        [SerializeField] Button bargainButton = null;
        [Header("Option Game Objects")]
        [SerializeField] GameObject statusPrefab = null;

        // Cached References
        BattleController battleController = null;
        BattleCanvas battleCanvas = null;
        Party party = null;

        protected override void Awake()
        {
            // Override default behavior, null implementation
        }

        protected override void Start()
        {
            // Override default behavior, null implementation
        }

        public void Setup(BattleController battleController, BattleCanvas battleCanvas, Party party)
        {
            this.battleController = battleController;
            this.battleCanvas = battleCanvas;
            this.party = party;

            SetGlobalCallbacks(battleController); // input handled via player controller, immediate override
        }

        public void InitiateCombat() // Called via unity events
        {
            battleController.SetBattleState(BattleState.Combat);
            gameObject.SetActive(false);
        }

        public void OpenStats() // Called via unity events
        {
            handleGlobalInput = false;
            GameObject childOption = Instantiate(statusPrefab, battleCanvas.transform);
            StatusBox statusBox = childOption.GetComponent<StatusBox>();
            statusBox.Setup(battleController, party);
            statusBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
        }

        public void AttemptToRun() // Called via unity events
        {
            // TODO:  add logic for running (odds vs. speed, etc.) -- should be calculated on party / combat participants in battle controller
            battleController.SetBattleOutcome(BattleOutcome.Ran);
            battleController.SetBattleState(BattleState.Outro);
            gameObject.SetActive(false);
        }
    }
}