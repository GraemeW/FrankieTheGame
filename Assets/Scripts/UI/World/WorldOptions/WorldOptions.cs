using Frankie.Combat;
using Frankie.Combat.UI;
using Frankie.Core;
using Frankie.Stats;
using Frankie.Stats.UI;
using Frankie.Control;
using UnityEngine;
using UnityEngine.UI;
using Frankie.Inventory.UI;
using System.Collections.Generic;

namespace Frankie.Speech.UI
{
    public class WorldOptions : DialogueOptionBox
    {
        // Tunables
        [SerializeField] Transform characterPanelTransform = null;
        [Header("Option Game Objects")]
        [SerializeField] GameObject characterSlidePrefab = null;
        [SerializeField] GameObject knapsackPrefab = null;
        [SerializeField] GameObject abilitiesPrefab = null;
        [SerializeField] GameObject statusPrefab = null;
        [SerializeField] GameObject mapPrefab = null;

        // State
        List<CharacterSlide> characterSlides = new List<CharacterSlide>();

        // Cached References
        PlayerStateHandler playerStateHandler = null;
        PlayerController playerController = null;
        WorldCanvas worldCanvas = null;
        Party party = null;
        GameObject childOption = null;

        protected override void Awake()
        {
            base.Awake();
            worldCanvas = GameObject.FindGameObjectWithTag("WorldCanvas").GetComponent<WorldCanvas>();
            playerStateHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStateHandler>();
            playerController = playerStateHandler.GetComponent<PlayerController>();
            party = playerStateHandler.GetComponent<Party>();
        }

        protected override void Start()
        {
            SetGlobalCallbacks(playerController); // input handled via player controller, immediate override
            SetupCharacterSlides();
        }

        private void SetupCharacterSlides()
        {
            characterSlides.Clear();
            foreach (CombatParticipant character in party.GetParty())
            {
                GameObject characterObject = Instantiate(characterSlidePrefab, characterPanelTransform);
                CharacterSlide characterSlide = characterObject.GetComponent<CharacterSlide>();
                characterSlide.SetCombatParticipant(character);
                characterSlides.Add(characterSlide);
            }
        }

        private void OnDestroy()
        {
            if (childOption != null) { Destroy(childOption); }
            foreach (Transform childCharacterPanel in characterPanelTransform)
            {
                Destroy(childCharacterPanel.gameObject);
            }
            playerStateHandler.ExitWorldOptions();
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

        public void OpenKnapsack() // Called via Unity Events
        {
            handleGlobalInput = false;
            GameObject childOption = Instantiate(knapsackPrefab, worldCanvas.gameObject.transform);
            InventoryBox inventoryBox = childOption.GetComponent<InventoryBox>();
            inventoryBox.Setup(playerController, party, characterSlides);
            inventoryBox.SetDisableCallback(this, DIALOGUE_CALLBACK_ENABLE_INPUT);
        }
    }
}
