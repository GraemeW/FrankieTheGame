using Frankie.Combat;
using Frankie.Combat.UI;
using Frankie.Speech.UI;
using Frankie.Stats;
using Frankie.Stats.UI;
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
        [SerializeField] Transform characterPanelTransform = null;
        [Header("Option Game Objects")]
        [SerializeField] GameObject characterSlidePrefab = null;
        [SerializeField] GameObject knapsackPrefab = null;
        [SerializeField] GameObject abilitiesPrefab = null;
        [SerializeField] GameObject statusPrefab = null;
        [SerializeField] GameObject mapPrefab = null;

        // Cached References
        PlayerController playerController = null;
        WorldCanvas worldCanvas = null;
        Party party = null;
        GameObject childOption = null;

        protected override void Awake()
        {
            base.Awake();
            playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
            worldCanvas = GameObject.FindGameObjectWithTag("WorldCanvas").GetComponent<WorldCanvas>();
            party = playerController.GetComponent<Party>();
        }

        protected override void Start()
        {
            SetGlobalCallbacks(playerController); // input handled via player controller, immediate override
            SetupCharacterSlides();
        }

        private void SetupCharacterSlides()
        {
            foreach (CombatParticipant character in party.GetParty())
            {
                GameObject characterObject = Instantiate(characterSlidePrefab, characterPanelTransform);
                CharacterSlide characterSlide = characterObject.GetComponent<CharacterSlide>();
                characterSlide.SetCombatParticipant(character);
            }
        }

        private void OnDestroy()
        {
            if (childOption != null) { Destroy(childOption); }
            foreach (Transform childCharacterPanel in characterPanelTransform)
            {
                Destroy(childCharacterPanel.gameObject);
            }
            playerController.ExitWorldOptions();
        }

        public override void HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return; }

            if (playerInputType == PlayerInputType.Option || playerInputType == PlayerInputType.Cancel)
            {
                if (childOption != null)
                {
                    Destroy(childOption);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            base.HandleGlobalInput(playerInputType);
        }

        public void OpenStatus() // Called via Unity Events
        {
            handleGlobalInput = false;
            GameObject childOption = Instantiate(statusPrefab, worldCanvas.gameObject.transform);
            StatusBox statusBox = childOption.GetComponent<StatusBox>();
            statusBox.Setup(playerController, party);
            statusBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
        }
    }
}
