using Frankie.Combat;
using Frankie.Stats;
using Frankie.Utils.UI;
using Frankie.Combat.UI;
using Frankie.Stats.UI;
using Frankie.Inventory.UI;
using Frankie.Control;
using UnityEngine;
using System.Collections.Generic;
using Frankie.ZoneManagement.UI;

namespace Frankie.Menu.UI
{
    public class WorldOptions : UIBox
    {
        // Tunables
        [SerializeField] Transform characterPanelTransform = null;
        [Header("Option Game Objects")]
        [SerializeField] GameObject characterSlidePrefab = null;
        [SerializeField] GameObject knapsackPrefab = null;
        [SerializeField] GameObject equipmentPrefab = null;
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

        private void Awake()
        {
            worldCanvas = GameObject.FindGameObjectWithTag("WorldCanvas").GetComponent<WorldCanvas>();
            playerStateHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStateHandler>();
            playerController = playerStateHandler.GetComponent<PlayerController>();
            party = playerStateHandler.GetComponent<Party>();
        }

        private void Start()
        {
            TakeControl(playerController, this, null); // input handled via player controller, immediate override
            SetupCharacterSlides();
            HandleClientEntry();
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

        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return true; } // Spoof:  Cannot accept input, so treat as if global input already handled

            if (playerInputType == PlayerInputType.Option || playerInputType == PlayerInputType.Cancel)
            {
                if (childOption != null)
                {
                    Destroy(childOption);
                    return true;
                }
            }
            return base.HandleGlobalInput(playerInputType);
        }

        public void OpenStatus() // Called via Unity Events
        {
            ResetWorldOptions();
            childOption = Instantiate(statusPrefab, worldCanvas.GetWorldOptionsParent());
            StatusBox statusBox = childOption.GetComponent<StatusBox>();
            statusBox.Setup(party);
            PassControl(statusBox);
        }

        public void OpenKnapsack() // Called via Unity Events
        {
            ResetWorldOptions();
            childOption = Instantiate(knapsackPrefab, worldCanvas.GetWorldOptionsParent());
            InventoryBox inventoryBox = childOption.GetComponent<InventoryBox>();
            inventoryBox.Setup(playerController, party, characterSlides);
            PassControl(inventoryBox);
        }

        public void OpenEquipment() // Called via Unity Events
        {
            ResetWorldOptions();
            childOption = Instantiate(equipmentPrefab, worldCanvas.GetWorldOptionsParent());
            EquipmentBox equipmentBox = childOption.GetComponent<EquipmentBox>();
            equipmentBox.Setup(playerController, party, characterSlides);
            PassControl(equipmentBox);
        }

        public void OpenMap() // Called via Unity Events
        {
            ResetWorldOptions();
            childOption = Instantiate(mapPrefab, worldCanvas.GetWorldOptionsParent());
            MapSuper mapSuper = childOption.GetComponent<MapSuper>();
            PassControl(mapSuper);
        }

        private void ResetWorldOptions()
        {
            childOption = null;
            worldCanvas.DestroyExistingWorldOptions();
            foreach (CharacterSlide characterSlide in characterSlides)
            {
                characterSlide.HighlightSlide(CombatParticipantType.Character, false);
            }
            handleGlobalInput = true;
        }
    }
}
