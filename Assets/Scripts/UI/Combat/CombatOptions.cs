using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Frankie.Combat.BattleController;

namespace Frankie.Combat.UI
{
    public class CombatOptions : MonoBehaviour
    {
        // Tunables
        [SerializeField] Button fightButton = null;
        [SerializeField] Button itemButton = null;
        [SerializeField] Button statsButton = null;
        [SerializeField] Button runButton = null;
        [SerializeField] Button bargainButton = null;

        // Cached References
        BattleController battleController = null;
        BattleCanvas battleCanvas = null;

        private void Awake()
        {
            battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();
            battleCanvas = FindObjectOfType<BattleCanvas>();
        }

        public void InitiateCombat()
        {
            battleController.SetBattleState(BattleState.Combat);
            gameObject.SetActive(false);
        }

        public void AttemptToRun()
        {
            // TODO:  Implement actual logic / odds calculations against escape
            battleController.SetBattleOutcome(BattleOutcome.Ran);
            battleController.SetBattleState(BattleState.Outro);
        }
    }
}