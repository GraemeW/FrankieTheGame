using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Core;
using Frankie.Control;
using Frankie.Combat;
using Frankie.Stats;
using Frankie.World;
using Frankie.Combat.UI;
using Frankie.Stats.UI;
using Frankie.Inventory.UI;
using Frankie.ZoneManagement.UI;
using Frankie.Utils.UI;
using Frankie.Utils.Localization;


namespace Frankie.Menu.UI
{
    public class WorldOptions : UIBox, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedKnapsackText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOutfitText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedAbilitiesText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedStatusText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMapText;
        [Header("Hookups")]
        [SerializeField] private Transform characterPanelTransform;
        [SerializeField] private UIChoiceButton knapsackOptionField;
        [SerializeField] private UIChoiceButton outfitOptionField;
        [SerializeField] private UIChoiceButton abilitiesOptionField;
        [SerializeField] private UIChoiceButton statusOptionField;
        [SerializeField] private UIChoiceButton mapOptionField;
        [Header("Prefabs")]
        [SerializeField] private CharacterSlide characterSlidePrefab;
        [SerializeField] private WalletUI walletUIPrefab;
        [SerializeField] private InventoryBox inventoryBoxPrefab;
        [SerializeField] private EquipmentBox equipmentBoxPrefab;
        [SerializeField] private AbilitiesBox abilitiesBoxPrefab;
        [SerializeField] private StatusBox statusBoxPrefab;
        [SerializeField] private MapSuper mapSuperPrefab;

        // State
        private readonly List<CharacterSlide> characterSlides = new();
        private readonly List<BattleEntity> partyBattleEntities = new();
        private WalletUI walletUI;
        private GameObject childOption;

        // Cached References
        private PlayerStateMachine playerStateMachine;
        private PlayerController playerController;
        private WorldCanvas worldCanvas;
        private PartyCombatConduit partyCombatConduit;
        
        #region UnityMethods
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

            InitializeLocalization();
            SetupBattleEntities();
            SetupCharacterSlides();
            SetupWallet();
            HandleClientEntry();
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
        #endregion
        
        #region LocalizationMethods

        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedKnapsackText.TableEntryReference,
                localizedOutfitText.TableEntryReference,
                localizedAbilitiesText.TableEntryReference,
                localizedStatusText.TableEntryReference,
                localizedMapText.TableEntryReference,
            };
        }
        
        private void InitializeLocalization()
        {
            if (knapsackOptionField != null) { knapsackOptionField.SetText(localizedKnapsackText.GetSafeLocalizedString()); }
            if (outfitOptionField != null) { outfitOptionField.SetText(localizedOutfitText.GetSafeLocalizedString()); }
            if (abilitiesOptionField != null) { abilitiesOptionField.SetText(localizedAbilitiesText.GetSafeLocalizedString()); }
            if (statusOptionField != null) { statusOptionField.SetText(localizedStatusText.GetSafeLocalizedString()); }
            if (mapOptionField != null) { mapOptionField.SetText(localizedMapText.GetSafeLocalizedString()); }
        }
        #endregion

        #region PublicMethods
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
        #endregion

        #region PrivateMethods
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
        #endregion

        #region InputHandling
        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return true; } // Spoof:  Cannot accept input, so treat as if global input already handled

            if (playerInputType is PlayerInputType.Option or PlayerInputType.Cancel)
            {
                if (childOption != null)
                {
                    Destroy(childOption);
                    return true;
                }
            }
            return base.HandleGlobalInput(playerInputType);
        }
        #endregion
    }
}
