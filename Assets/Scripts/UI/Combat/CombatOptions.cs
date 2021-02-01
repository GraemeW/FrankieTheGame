using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Frankie.Combat.BattleController;

namespace Frankie.Combat.UI
{
    public class CombatOptions : MonoBehaviour
    {
        // TODO:  this should just extend dialogue box rather than have its own class, then use choose functionality

        // Tunables -- TODO:  Linked up to use via keyboard command
        [SerializeField] Button fightButton = null;
        [SerializeField] Button itemButton = null;
        [SerializeField] Button statsButton = null;
        [SerializeField] Button runButton = null;
        [SerializeField] Button bargainButton = null;

        // Cached References
        BattleController battleController = null;

        private void Awake()
        {
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