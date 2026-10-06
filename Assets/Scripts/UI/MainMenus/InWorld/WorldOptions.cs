using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Control;
using LowDefMustard.UIBox;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Core;
using Frankie.Control;
using Frankie.Combat;
using Frankie.Stats;
using Frankie.World;
using Frankie.Combat.UI;
using Frankie.Stats.UI;
using Frankie.Inventory.UI;
using Frankie.Zones.UI;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class WorldOptions : UIBox<UIBoxState>, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedKnapsackText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOutfitText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedAbilitiesText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedStatusText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMapText;
        [SerializeField] private CharacterSlideText characterSlideText;
        [Header("Prefabs")]
        [SerializeField] private WalletUI walletUIPrefab;
        [SerializeField] private InventoryBox inventoryBoxPrefab;
        [SerializeField] private EquipmentBox equipmentBoxPrefab;
        [SerializeField] private AbilitiesBox abilitiesBoxPrefab;
        [SerializeField] private StatusBox statusBoxPrefab;
        [SerializeField] private MapSuper mapSuperPrefab;

        // State
        private readonly List<CharacterSlideHandle> characterSlides = new();
        private WalletUI walletUI;
        private GameObject childOption;

        // Cached References
        private UIToolkitMenuView menuView;
        private PlayerStateMachine playerStateMachine;
        private PlayerController playerController;
        private WorldCanvas worldCanvas;
        private PartyCombatConduit partyCombatConduit;

        // UIBox Configuration
        protected override EnumLookup<UIBoxState,UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var worldOptionsConfiguration =  new EnumLookup<UIBoxState,UIBoxStateBehaviour>();
            var defaultStateBehaviour = new UIBoxStateBehaviour(
                setupChoiceOptions: ImplementSetUpChoiceOptions,
                tryHandleBackNavigation: ImplementTryHandleBackNavigation);
            worldOptionsConfiguration.TrySet(UIBoxState.Default, defaultStateBehaviour);
            return worldOptionsConfiguration;
        }

        #region UnityMethods
        protected override bool TryAcquireDependencies()
        {
            worldCanvas = WorldCanvas.FindWorldCanvas();
            playerStateMachine = Player.FindPlayerStateMachine();
            if (worldCanvas == null || playerStateMachine == null) { return false; }

            playerController = playerStateMachine.GetComponent<PlayerController>();
            partyCombatConduit = playerStateMachine.GetComponent<PartyCombatConduit>();
            if (playerController == null) { return false; }

            playerController.AddInputReceiver(this, null);
            return true;
        }

        protected override void AwakeTriggered()
        {
            menuView = GetComponent<UIToolkitMenuView>();
            keepPointerInputWhenInactive = true; // Choosing another option swaps out the open child box (and slides are click targets for it)
        }

        protected override void StartTriggered()
        {
            SetupCharacterSlides();
            SetupWallet();
        }

        protected override void DestroyTriggered()
        {
            if (childOption != null) { Destroy(childOption); }
            foreach (CharacterSlideHandle characterSlide in characterSlides) { characterSlide.Release(); }
            characterSlides.Clear();
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
        #endregion

        #region PublicMethods
        public void OpenStatus()
        {
            ResetWorldOptions();
            StatusBox statusBox = Instantiate(statusBoxPrefab, worldCanvas.GetWorldOptionsParent());
            childOption = statusBox.gameObject;
            statusBox.Setup(partyCombatConduit);
            controller.AddInputReceiver(statusBox, null);
        }

        public void OpenKnapsack()
        {
            ResetWorldOptions();
            InventoryBox inventoryBox = Instantiate(inventoryBoxPrefab, worldCanvas.GetWorldOptionsParent());
            childOption = inventoryBox.gameObject;
            inventoryBox.Setup(playerController, partyCombatConduit, GetCharacterSlides());
            controller.AddInputReceiver(inventoryBox, null);
        }

        public void OpenEquipment()
        {
            ResetWorldOptions();
            EquipmentBox equipmentBox = Instantiate(equipmentBoxPrefab, worldCanvas.GetWorldOptionsParent());
            childOption = equipmentBox.gameObject;
            equipmentBox.Setup(playerController, partyCombatConduit, GetCharacterSlides());
            controller.AddInputReceiver(equipmentBox, null);
        }

        public void OpenMap()
        {
            ResetWorldOptions();
            MapSuper mapSuper = Instantiate(mapSuperPrefab, worldCanvas.GetWorldOptionsParent());
            childOption = mapSuper.gameObject;
            controller.AddInputReceiver(mapSuper, null);
        }

        public void OpenAbilities()
        {
            ResetWorldOptions();
            AbilitiesBox abilitiesBox = Instantiate(abilitiesBoxPrefab, worldCanvas.GetWorldOptionsParent());
            childOption = abilitiesBox.gameObject;
            abilitiesBox.Setup(playerController, partyCombatConduit, GetCharacterSlides());
            controller.AddInputReceiver(abilitiesBox, null);
        }
        #endregion

        #region ProtectedPrivateMethods
        private void ImplementSetUpChoiceOptions()
        {
            if (choiceOptions.Count == 0)
            {
                AddNonDestroyChoiceOption(localizedKnapsackText.GetSafeLocalizedString(), OpenKnapsack);
                AddNonDestroyChoiceOption(localizedOutfitText.GetSafeLocalizedString(), OpenEquipment);
                AddNonDestroyChoiceOption(localizedAbilitiesText.GetSafeLocalizedString(), OpenAbilities);
                AddNonDestroyChoiceOption(localizedStatusText.GetSafeLocalizedString(), OpenStatus);
                AddNonDestroyChoiceOption(localizedMapText.GetSafeLocalizedString(), OpenMap);
            }
            ReconcileChoiceOptions();
        }

        private List<ICharacterSlide> GetCharacterSlides() => characterSlides.Cast<ICharacterSlide>().ToList();
        
        private void SetupCharacterSlides()
        {
            foreach (CombatParticipant combatParticipant in partyCombatConduit.GetPartyCombatParticipants())
            {
                var characterSlide = new CharacterSlideHandle(menuView, new BattleEntity(combatParticipant), characterSlideText);
                menuView.AddEntry(characterSlide);
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
            foreach (CharacterSlideHandle characterSlide in characterSlides)
            {
                characterSlide.HighlightSlide(BattleEntitySelectionType.Actor, false);
            }
        }
        #endregion

        #region InputHandling
        private bool ImplementTryHandleBackNavigation(ControllerInputType controllerInputType)
        {
            if (childOption == null) { return false; }
            Destroy(childOption);
            return true;
        }
        #endregion
    }
}
