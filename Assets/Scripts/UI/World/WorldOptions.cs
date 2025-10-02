using UnityEngine;
using System.Collections.Generic;
using Frankie.ZoneManagement.UI;
using Frankie.Core;
using Frankie.Combat;
using Frankie.Stats;
using Frankie.Utils.UI;
using Frankie.Combat.UI;
using Frankie.Stats.UI;
using Frankie.Inventory.UI;
using Frankie.Control;

namespace Frankie.Menu.UI
{
    public class WorldOptions : UIBox
    {
        // Tunables
        [SerializeField] Transform characterPanelTransform = null;
        [Header("Option Game Objects")]
        [SerializeField] CharacterSlide characterSlidePrefab = null;
        [SerializeField] WalletUI walletUIPrefab = null;
        [SerializeField] InventoryBox inventoryBoxPrefab = null;
        [SerializeField] EquipmentBox equipmentBoxPrefab = null;
        [SerializeField] AbilitiesBox abilitiesBoxPrefab = null;
        [SerializeField] StatusBox statusBoxPrefab = null;
        [SerializeField] MapSuper mapSuperPrefab = null;

        // State
        List<CharacterSlide> characterSlides = new List<CharacterSlide>();
        List<BattleEntity> partyBattleEntities = new List<BattleEntity>();
        WalletUI walletUI = null;
        GameObject childOption = null;

        // Cached References
        PlayerStateMachine playerStateMachine = null;
        PlayerController playerController = null;
        WorldCanvas worldCanvas = null;
        PartyCombatConduit partyCombatConduit = null;
        
        private void Awake()
        {
            worldCanvas = WorldCanvas.FindWorldCanvas();
            playerStateMachine = Player.FindPlayerStateMachine();
            if (worldCanvas == null || playerStateMachine == null) { Destroy(gameObject); }

            playerController = playerStateMachine?.GetComponent<PlayerController>();
            partyCombatConduit = playerStateMachine?.GetComponent<PartyCombatConduit>();
        }

        private void Start()
        {
            TakeControl(playerController, this, null); // input handled via player controller, immediate override

            SetupBattleEntities();
            SetupCharacterSlides();
            SetupWallet();
            HandleClientEntry();
        }

        private void SetupBattleEntities()
        {
            partyBattleEntities.Clear();
            foreach (CombatParticipant combatParticipant in partyCombatConduit.GetPartyCombatParticipants())
            {
                partyBattleEntities.Add(new BattleEntity(combatParticipant));
            }
        }

        private void SetupCharacterSlides()
        {
            characterSlides.Clear();
            foreach (BattleEntity battleEntity in partyBattleEntities)
            {
                CharacterSlide characterSlide = Instantiate(characterSlidePrefab, characterPanelTransform);
                characterSlide.SetBattleEntity(battleEntity);
                characterSlides.Add(characterSlide);
            }
        }

        private void SetupWallet()
        {
            walletUI = Instantiate(walletUIPrefab, worldCanvas.transform);
        }

        private void OnDestroy()
        {
            if (childOption != null) { Destroy(childOption); }
            foreach (Transform childCharacterPanel in characterPanelTransform)
            {
                Destroy(childCharacterPanel.gameObject);
            }
            if (walletUI != null) { Destroy(walletUI.gameObject); }
            playerStateMachine?.EnterWorld();
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
            StatusBox statusBox = Instantiate(statusBoxPrefab, worldCanvas.GetWorldOptionsParent());
            childOption = statusBox.gameObject;
            statusBox.Setup(partyCombatConduit);
            PassControl(statusBox);
        }

        public void OpenKnapsack() // Called via Unity Events
        {
            ResetWorldOptions();
            InventoryBox inventoryBox = Instantiate(inventoryBoxPrefab, worldCanvas.GetWorldOptionsParent());
            childOption = inventoryBox.gameObject;
            inventoryBox.Setup(playerController, partyCombatConduit, characterSlides);
            PassControl(inventoryBox);
        }

        public void OpenEquipment() // Called via Unity Events
        {
            ResetWorldOptions();
            EquipmentBox equipmentBox = Instantiate(equipmentBoxPrefab, worldCanvas.GetWorldOptionsParent());
            childOption = equipmentBox.gameObject;
            equipmentBox.Setup(playerController, partyCombatConduit, characterSlides);
            PassControl(equipmentBox);
        }

        public void OpenMap() // Called via Unity Events
        {
            ResetWorldOptions();
            MapSuper mapSuper = Instantiate(mapSuperPrefab, worldCanvas.GetWorldOptionsParent());
            childOption = mapSuper.gameObject;
            PassControl(mapSuper);
        }

        public void OpenAbilities() // Called via Unity Events
        {
            ResetWorldOptions();
            AbilitiesBox abilitiesBox = Instantiate(abilitiesBoxPrefab, worldCanvas.GetWorldOptionsParent());
            childOption = abilitiesBox.gameObject;
            abilitiesBox.Setup(playerController, partyCombatConduit, characterSlides);
            PassControl(abilitiesBox);
        }

        private void ResetWorldOptions()
        {
            childOption = null;
            worldCanvas.DestroyExistingWorldOptions();
            foreach (CharacterSlide characterSlide in characterSlides)
            {
                characterSlide.HighlightSlide(CombatParticipantType.Friendly, false);
            }
            handleGlobalInput = true;
        }
    }
}
