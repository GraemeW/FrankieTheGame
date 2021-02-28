using Frankie.Speech.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Control.UI
{
    public class WorldOptions : DialogueOptionBox
    {
        [SerializeField] Button knapsackButton = null;
        [SerializeField] Button abilitiesButton = null;
        [SerializeField] Button statusButton = null;
        [SerializeField] Button mapButton = null;


        // Cached References
        PlayerController playerController = null;

        protected override void Awake()
        {
            base.Awake();
            playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            playerController.globalInput += HandleGlobalInput;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            playerController.globalInput -= HandleGlobalInput;
        }
    }
}
