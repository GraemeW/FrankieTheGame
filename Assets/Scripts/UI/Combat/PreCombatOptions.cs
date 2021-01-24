using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Combat.UI
{
    public class PreCombatOptions : MonoBehaviour
    {
        // Tunables
        [SerializeField] Button fightButton = null;
        [SerializeField] Button itemButton = null;
        [SerializeField] Button statsButton = null;
        [SerializeField] Button runButton = null;
        [SerializeField] Button bargainButton = null;

        // Cached References
        [SerializeField] BattleHandler battleHandler = null;
        [SerializeField] BattleCanvas battleCanvas = null;

        private void Awake()
        {
            battleHandler = FindObjectOfType<BattleHandler>();
            battleCanvas = FindObjectOfType<BattleCanvas>();
        }

        private void OnEnable()
        {
            // TODO:  Add button hookups
        }
    }
}