using Frankie.Speech.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Control.UI
{
    public class WorldOptions : DialogueOptionBox
    {
        // Tunables
        [SerializeField] Button knapsackButton = null;
        [SerializeField] Button abilitiesButton = null;
        [SerializeField] Button statusButton = null;
        [SerializeField] Button mapButton = null;
        [Header("Option Game Objects")]
        [SerializeField] GameObject knapsackPrefab = null;
        [SerializeField] GameObject abilitiesPrefab = null;
        [SerializeField] GameObject statusPrefab = null;
        [SerializeField] GameObject mapPrefab = null;

        // Cached References
        PlayerController playerController = null;

        protected override void Awake()
        {
            base.Awake();
            playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        }

        protected override void Start()
        {
            SetGlobalCallbacks(playerController); // input handled via player controller, immediate override
        }

        private void OnDestroy()
        {
            playerController.SetPlayerState(PlayerState.inWorld);
        }

        public void OpenStatus()
        {
            handleGlobalInput = false;
            //GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, infoChooseParent);
        }
    }
}
