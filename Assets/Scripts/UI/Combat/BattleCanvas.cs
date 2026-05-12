using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Core;
using Frankie.Stats;
using Frankie.Inventory;
using Frankie.Utils;
using Frankie.Speech.UI;
using Frankie.Inventory.UI;
using Frankie.Utils.UI;
using Frankie.Utils.Localization;

namespace Frankie.Combat.UI
{
    public class BattleCanvas : MonoBehaviour, IUIBoxCallbackReceiver, ILocalizable
    {
        // Tunables
        [Header("Parents")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Transform playerPanelParent;
        [SerializeField] private Transform topRowParent;
        [SerializeField] private List<Transform> topRowColumns = new();
        [SerializeField] private Transform midRowParent;
        [SerializeField] private List<Transform> midRowColumns = new();
        [SerializeField] private Transform bottomRowParent;
        [SerializeField] private List<Transform> bottomRowColumns = new();
        [SerializeField] private Image backgroundFill;
        [SerializeField] private MovingBackgroundProperties defaultMovingBackgroundProperties;
        [SerializeField] private Transform infoChooseParent;
        [SerializeField] private CombatLog combatLog;
        [SerializeField] private CombatOptions combatOptions;
        [SerializeField] private SkillSelectionUI skillSelection;

        [Header("Prefabs")]
        [SerializeField] private CharacterSlide characterSlidePrefab;
        [SerializeField] private EnemySlide enemySlidePrefab;
        [SerializeField] private DialogueBox dialogueBoxPrefab;
        [SerializeField] private DialogueOptionBox dialogueOptionBoxPrefab;
        [SerializeField] private InventorySwapBox inventorySwapBoxPrefab;

        [Header("Encounter Messages")]
        [Header("{0} for enemy name")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageEncounterSingle;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageEncounterMultiple;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageEncounterPreHype;
        [Header("Post-Combat Messages")]
        [Header("Include {0} for experience value")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageGainedExperience;
        [Header("Include {0} for character name, {1} for level")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageCharacterLevelUp;
        [Header("Include {0} for stat name, {1} for value")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageCharacterStatGained;
        [Header("Loot Messages")]
        [Header("Include {0} for leader character name")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageGainedLoot;
        [Header("Include {0} for enemy name, {1} for item name")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageEnemyDroppedLoot;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageEnemyDroppedLootNoRoom;
        [Header("Include {0} for item name")] 
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageConfirmThrowOut;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionChuckItemAffirmative;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionChuckItemNegative;
        [Header("Exit Combat Messages")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageBattleCompleteWon;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageBattleCompleteLost;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageBattleCompleteRan;
        
        // State
        private readonly Dictionary<BattleEntity, EnemySlide> enemySlideLookup = new();
        private BattleState lastBattleState = BattleState.Inactive;
        private readonly Queue<Action> queuedUISequences = new();
        private readonly List<CharacterLevelUpSheetPair> queuedLevelUps = new();
        private bool firstCharacterToggle = false;
        private bool busyWithSerialAction = false;
        private bool outroQueued = false;

        // Cached References
        private PartyCombatConduit partyCombatConduit;
        private BattleController battleController;
        private BattleRewards battleRewards;

        // Data Structures
        private struct CharacterLevelUpSheetPair
        {
            public BaseStats baseStats;
            public int level;
            public List<Tuple<string, int>> statNameValuePairs;
        }

        #region StaticFind
        private const string _battleCanvasTag = "BattleCanvas";

        public static BattleCanvas FindBattleCanvas()
        {
            var battleCanvasGameObject = GameObject.FindGameObjectWithTag(_battleCanvasTag);
            return battleCanvasGameObject != null ? battleCanvasGameObject.GetComponent<BattleCanvas>() : null;
        }
        #endregion

        #region UnityMethods
        private void Awake()
        {
            partyCombatConduit = Player.FindPlayerObject()?.GetComponent<PartyCombatConduit>();
            battleController = BattleController.FindBattleController();
            if (partyCombatConduit == null || battleController == null) { Destroy(gameObject); return; }

            battleRewards = battleController.GetBattleRewards();
            skillSelection.SetupBattleController(battleController);
            combatOptions.Setup(battleController, this, partyCombatConduit);
            combatOptions.TakeControl(battleController, this, null);

            ClearBattleCanvas();
        }

        private void OnEnable()
        {
            BattleEventBus<BattleStagingEvent>.SubscribeToEvent(HandleBattleStagingEvent);
            BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(Setup);
            BattleEventBus<BattleEntityAddedEvent>.SubscribeToEvent(SetupBattleEntity);
            BattleEventBus<BattleFadeTransitionEvent>.SubscribeToEvent(HandleBattleFadeTransitionEvent);
        }

        private void OnDisable()
        {
            BattleEventBus<BattleStagingEvent>.UnsubscribeFromEvent(HandleBattleStagingEvent);
            BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(Setup);
            BattleEventBus<BattleEntityAddedEvent>.UnsubscribeFromEvent(SetupBattleEntity);
            BattleEventBus<BattleFadeTransitionEvent>.UnsubscribeFromEvent(HandleBattleFadeTransitionEvent);
        }

        private void OnDestroy()
        {
            ClearBattleCanvas();
        }

        private void Update()
        {
            if (busyWithSerialAction || queuedUISequences.Count == 0) return;
            Action nextUISequence = queuedUISequences.Dequeue();
            StartSerialAction(nextUISequence);
        }
        #endregion

        #region PublicMethods
        public Canvas GetCanvas() => canvas;
        public EnemySlide GetEnemySlide(BattleEntity combatParticipant) => enemySlideLookup.GetValueOrDefault(combatParticipant);
        public DialogueBox SetupRunFailureMessage(IUIBoxCallbackReceiver callbackReceiver, Action[] actions)
        {
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);
            dialogueBox.AddText("Failed to run away.");
            dialogueBox.TakeControl(battleController, callbackReceiver, actions);

            return dialogueBox;
        }

        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedMessageEncounterSingle.TableEntryReference,
                localizedMessageEncounterMultiple.TableEntryReference,
                localizedMessageEncounterPreHype.TableEntryReference,
                localizedMessageGainedExperience.TableEntryReference,
                localizedMessageCharacterLevelUp.TableEntryReference,
                localizedMessageCharacterStatGained.TableEntryReference,
                localizedMessageGainedLoot.TableEntryReference,
                localizedMessageEnemyDroppedLoot.TableEntryReference,
                localizedMessageEnemyDroppedLootNoRoom.TableEntryReference,
                localizedMessageConfirmThrowOut.TableEntryReference,
                localizedOptionChuckItemAffirmative.TableEntryReference,
                localizedOptionChuckItemNegative.TableEntryReference,
                localizedMessageBattleCompleteWon.TableEntryReference,
                localizedMessageBattleCompleteLost.TableEntryReference,
                localizedMessageBattleCompleteRan.TableEntryReference
            };
        }
        #endregion

        #region PrivateSetupTeardownMethods

        private void HandleBattleStagingEvent(BattleStagingEvent battleStagingEvent)
        {
            switch (battleStagingEvent.battleStagingType)
            {
                case BattleStagingType.BattleSetUp:
                    if (!battleStagingEvent.optionalParametersSet) { return; }
                    SetupBackgroundFill(battleStagingEvent.GetEnemyEntities());
                    break;
                case BattleStagingType.BattleControllerPrimed:
                    if (!battleStagingEvent.optionalParametersSet) { return; }
                    SetupEntryMessage(battleStagingEvent.GetEnemyEntities());
                    break;
                case BattleStagingType.BattleTornDown:
                    break;
            }
        }
        
        private void Setup(BattleStateChangedEvent battleStateChangedEvent)
        {
            BattleState state = battleStateChangedEvent.battleState;
            BattleOutcome battleOutcome = battleStateChangedEvent.battleOutcome;

            if (state == lastBattleState) { return; } // Only act on state changes
            lastBattleState = state;

            switch (state)
            {
                case BattleState.PreCombat:
                    skillSelection.gameObject.SetActive(false);
                    combatOptions.EnableCombatOptions();
                    break;
                case BattleState.Combat:
                    if (combatLog != null)
                    {
                        combatLog.AddCombatLogText("  Combat Started . . . ");
                        combatLog.gameObject.SetActive(true);
                    }
                    combatOptions.gameObject.SetActive(false);
                    skillSelection.gameObject.SetActive(true);
                    break;
                case BattleState.Outro:
                case BattleState.Rewards:
                {
                    if (outroQueued) { return; }

                    if (combatLog != null) { combatLog.gameObject.SetActive(false); }
                    combatOptions.gameObject.SetActive(false);
                    skillSelection.gameObject.SetActive(false);
                    queuedUISequences.Enqueue(() => SetupExperienceMessage(battleOutcome));
                    SetupAllLootMessages(battleOutcome); // Parent function to queue a number of additional loot messages into UI sequences
                    queuedUISequences.Enqueue(() => SetupExitMessage(battleOutcome));
                    outroQueued = true;
                    break;
                }
            }
        }

        private void SetupBattleEntity(BattleEntityAddedEvent battleEntityAddedEvent)
        {
            BattleEntity battleEntity = battleEntityAddedEvent.battleEntity;

            if (!battleEntity.isAssistCharacter)
            {
                if (battleEntity.isCharacter)
                {
                    SetupCharacter(battleEntity);
                }
                else
                {
                    SetupEnemy(battleEntity);
                }
            }
            else
            {
                SetupAssistCharacter(battleEntity);
            }
        }

        private void SetupEnemy(BattleEntity battleEntity)
        {
            Transform spawnLocation;
            List<Transform> columnEntries = battleEntity.row switch
            {
                BattleRow.Middle => midRowColumns,
                BattleRow.Top => topRowColumns,
                BattleRow.Bottom => bottomRowColumns,
                _ => midRowColumns
            };
            
            if (battleEntity.column >= columnEntries.Count)
            {
                // Default to spawn directly in parent if column matching doesn't exist
                spawnLocation = battleEntity.row switch
                {
                    BattleRow.Middle => midRowParent,
                    BattleRow.Top => topRowParent,
                    BattleRow.Bottom => bottomRowParent,
                    _ => midRowParent
                };
                Debug.Log($"Warning -- battle entity column ({battleEntity.column}) out of index for BattleCanvas");
            }
            else { spawnLocation = columnEntries[battleEntity.column]; }
            
            EnemySlide enemySlide = Instantiate(enemySlidePrefab, spawnLocation);
            enemySlide.SetBattleEntity(battleEntity);
            if (combatLog != null) { combatLog.AddCombatListener(battleEntity.combatParticipant); }

            enemySlideLookup[battleEntity] = enemySlide;
        }

        private void SetupCharacter(BattleEntity character)
        {
            CharacterSlide characterSlide = Instantiate(characterSlidePrefab, playerPanelParent);
            characterSlide.SetBattleEntity(character);
            if (combatLog != null) { combatLog.AddCombatListener(character.combatParticipant); }

            if (!firstCharacterToggle)
            {
                characterSlide.GetComponent<Button>().Select();
                battleController.SetSelectedCharacter(character.combatParticipant);
                firstCharacterToggle = true;
            }
            if (character.combatParticipant.TryGetComponent(out BaseStats baseStats)) { baseStats.onLevelUp += HandleLevelUp; }
        }

        private void SetupAssistCharacter(BattleEntity assistCharacter)
        {
            // No panel + limited feedback & no level-ups for assist characters
            if (combatLog != null) { combatLog.AddCombatListener(assistCharacter.combatParticipant); }
        }

        private void SetupBackgroundFill(IList<BattleEntity> enemies)
        {
            IList<CombatParticipant> viableEnemies = CombatParticipant.GetPriorityCombatParticipants(enemies);
            int enemyIndex = UnityEngine.Random.Range(0, viableEnemies.Count);
            MovingBackgroundProperties movingBackgroundProperties = viableEnemies[enemyIndex].GetMovingBackgroundProperties();
            
            if (movingBackgroundProperties.tileSpriteImage == null || movingBackgroundProperties.shaderMaterial == null)
            {
                backgroundFill.sprite = defaultMovingBackgroundProperties.tileSpriteImage;
                backgroundFill.material = defaultMovingBackgroundProperties.shaderMaterial;
            }
            else
            {
                backgroundFill.sprite = movingBackgroundProperties.tileSpriteImage;
                backgroundFill.material = movingBackgroundProperties.shaderMaterial;
            }
        }
        
        private void HandleBattleFadeTransitionEvent(BattleFadeTransitionEvent battleFadeTransitionEvent)
        {
            if (battleFadeTransitionEvent.fadePhase != BattleFadePhase.ExitPeak) { return; }
            Destroy(gameObject);
        }
        
        private void ClearBattleCanvas()
        {
            foreach (Transform child in playerPanelParent) { Destroy(child.gameObject); }
            foreach (Transform columnEntry in topRowColumns) { foreach (Transform child in columnEntry) { Destroy(child.gameObject); } }
            foreach (Transform columnEntry in midRowColumns) { foreach (Transform child in columnEntry) { Destroy(child.gameObject); } }
            foreach (Transform columnEntry in bottomRowColumns) { foreach (Transform child in columnEntry) { Destroy(child.gameObject); } }
        }
        #endregion

        #region PrivateUtility
        private void StartSerialAction(Action action)
        {
            // !! It is the responsibility of the called action to reset busyWithSerialAction toggle !!
            if (action == null) return;
            busyWithSerialAction = true;
            action.Invoke();
        }

        private void HandleLevelUp(BaseStats baseStats, int level, Dictionary<Stat, float> levelUpSheet)
        {
            var statNameValuePairs = levelUpSheet.Select(entry => new Tuple<string, int>(LocalizationNames.GetLocalizedName(entry.Key), Mathf.RoundToInt(entry.Value))).ToList();
            var characterLevelUpSheetPair = new CharacterLevelUpSheetPair
            {
                baseStats = baseStats,
                level = level,
                statNameValuePairs = statNameValuePairs
            };
            queuedLevelUps.Add(characterLevelUpSheetPair);
        }

        private void SetupEntryMessage(IList<BattleEntity> enemies)
        {
            BattleEntity enemy = enemies.FirstOrDefault();
            if (enemy == null) { return; }
            
            var entryMessage = (enemies.Count > 1) ? 
                string.Format(localizedMessageEncounterMultiple.GetSafeLocalizedString(), enemy.combatParticipant.GetCombatName()) : 
                string.Format(localizedMessageEncounterSingle.GetSafeLocalizedString(), enemy.combatParticipant.GetCombatName());

            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);
            dialogueBox.AddText(entryMessage);
            dialogueBox.AddPageBreak();
            dialogueBox.AddText(localizedMessageEncounterPreHype.GetSafeLocalizedString());
            dialogueBox.TakeControl(battleController, this, new Action[] { () => battleController.SetBattleState(BattleState.PreCombat, BattleOutcome.Undetermined) });
        }

        private void SetupExperienceMessage(BattleOutcome battleOutcome)
        {
            if (battleOutcome != BattleOutcome.Won) { busyWithSerialAction = false; return; }

            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);
            dialogueBox.AddText(string.Format(localizedMessageGainedExperience.GetSafeLocalizedString(), Mathf.RoundToInt(battleRewards.GetBattleExperienceReward()).ToString()));

            foreach (CharacterLevelUpSheetPair characterLevelUpSheetPair in queuedLevelUps)
            {
                dialogueBox.AddPageBreak();

                dialogueBox.AddText(string.Format(localizedMessageCharacterLevelUp.GetSafeLocalizedString(), characterLevelUpSheetPair.baseStats.GetCharacterProperties().GetCharacterDisplayName(), characterLevelUpSheetPair.level.ToString()));
                int pageClearReset = 0;

                foreach (Tuple<string, int> statNameValuePair in characterLevelUpSheetPair.statNameValuePairs)
                {
                    if (Mathf.RoundToInt(statNameValuePair.Item2) == 0) { continue; }
                    
                    if (pageClearReset > 2)
                    {
                        dialogueBox.AddPageBreak();
                        pageClearReset = 0;
                    }

                    dialogueBox.AddText(string.Format(localizedMessageCharacterStatGained.GetSafeLocalizedString(), statNameValuePair.Item1, statNameValuePair.Item2.ToString()));
                    pageClearReset++;
                }

                characterLevelUpSheetPair.baseStats.onLevelUp -= HandleLevelUp;
                    // Unsubscribe to messages -- not the cleanest location, but the only one available
            }

            dialogueBox.TakeControl(battleController, this, new Action[] { () => busyWithSerialAction = false });
        }

        private void SetupAllLootMessages(BattleOutcome battleOutcome)
        {
            queuedUISequences.Enqueue(() => SetupPreLootMessage(battleOutcome));
            queuedUISequences.Enqueue(() => SetupAllocatedLootMessage(battleOutcome));
            foreach (Tuple<string, InventoryItem> enemyItemPair in battleRewards.GetUnallocatedLootCart())
            {
                queuedUISequences.Enqueue(() => SetupUnallocatedLootMessage(enemyItemPair.Item1, enemyItemPair.Item2, battleOutcome));
            }
        }

        private void SetupPreLootMessage(BattleOutcome battleOutcome)
        {
            if (battleOutcome != BattleOutcome.Won || !battleRewards.HasLootCart()) { busyWithSerialAction = false; return; }

            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);
            dialogueBox.AddText(string.Format(localizedMessageGainedLoot.GetSafeLocalizedString(), partyCombatConduit.GetPartyLeaderName()));

            dialogueBox.TakeControl(battleController, this, new Action[] { () => busyWithSerialAction = false });
        }

        private void SetupAllocatedLootMessage(BattleOutcome battleOutcome)
        {
            // Handling items that were easily placed in inventory
            if (battleOutcome != BattleOutcome.Won) { busyWithSerialAction = false; return; }
            if (!battleRewards.HasAllocatedLootCart()) { busyWithSerialAction = false; return; }
            
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);

            foreach (Tuple<string, InventoryItem> enemyItemPair in battleRewards.GetAllocatedLootCart())
            {
                dialogueBox.AddText(string.Format(localizedMessageEnemyDroppedLoot.GetSafeLocalizedString(), enemyItemPair.Item1, enemyItemPair.Item2.GetDisplayName()));
                dialogueBox.AddPageBreak();
            }

            dialogueBox.TakeControl(battleController, this, new Action[] { () => busyWithSerialAction = false });
        }

        private void SetupUnallocatedLootMessage(string enemyName, InventoryItem inventoryItem, BattleOutcome battleOutcome)
        {
            // Handling items that cannot be placed in inventory due to inventory full
            // Note:  busyWithSerialAction is reset on resolution of sub-menu calls
            // -- See SetupInventorySwapBox && SetupConfirmThrowOutItemMessage

            if (battleOutcome != BattleOutcome.Won) { busyWithSerialAction = false; return; }
            if (!battleRewards.HasUnallocatedLootCart()) { busyWithSerialAction = false; return; }

            DialogueOptionBox dialogueOptionBox = Instantiate(dialogueOptionBoxPrefab, infoChooseParent);
            dialogueOptionBox.Setup(string.Format(localizedMessageEnemyDroppedLootNoRoom.GetSafeLocalizedString(), enemyName, inventoryItem.GetDisplayName()));

            var choiceActionPairs = new List<ChoiceActionPair>
            {
                new(localizedOptionChuckItemAffirmative.GetSafeLocalizedString(), () => { SetupInventorySwapBox(enemyName, inventoryItem, battleOutcome); Destroy(dialogueOptionBox.gameObject); } ),
                new(localizedOptionChuckItemNegative.GetSafeLocalizedString(), () => { SetupConfirmThrowOutItemMessage(enemyName, inventoryItem, battleOutcome); Destroy(dialogueOptionBox.gameObject); } )
            };
            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);

            dialogueOptionBox.ClearDisableCallbacksOnChoose(true); // Clear window re-spawn (see below) on successful choice selection
            dialogueOptionBox.TakeControl(battleController, this, new Action[] { () => SetupUnallocatedLootMessage(enemyName, inventoryItem, battleOutcome) }); // If user tabs out of this window, re-spawn it (avoid lost loot)
        }

        private void SetupInventorySwapBox(string enemyName, InventoryItem inventoryItem, BattleOutcome battleOutcome)
        {
            InventorySwapBox inventorySwapBox = Instantiate(inventorySwapBoxPrefab, infoChooseParent);
            inventorySwapBox.Setup(battleController, partyCombatConduit, inventoryItem, () => { inventorySwapBox.ClearDisableCallbacks(); busyWithSerialAction = false; });
                // Inventory box destruction handled by swap box on successful swap

            inventorySwapBox.TakeControl(battleController, this, new Action[] { () => SetupUnallocatedLootMessage(enemyName, inventoryItem, battleOutcome) });
            // If user tabs out of this window, re-spawn it (avoid lost loot)
        }

        private void SetupConfirmThrowOutItemMessage(string enemyName, InventoryItem inventoryItem, BattleOutcome battleOutcome)
        {
            DialogueOptionBox dialogueOptionBox = Instantiate(dialogueOptionBoxPrefab, infoChooseParent);
            dialogueOptionBox.Setup(string.Format(localizedMessageConfirmThrowOut.GetSafeLocalizedString(), inventoryItem.GetDisplayName()));

            var choiceActionPairs = new List<ChoiceActionPair>
            {
                new(localizedOptionChuckItemAffirmative.GetSafeLocalizedString(), () => { dialogueOptionBox.ClearDisableCallbacks(); busyWithSerialAction = false; Destroy(dialogueOptionBox); }), // Exit and close out serial action
                new(localizedOptionChuckItemNegative.GetSafeLocalizedString(), () => { Destroy(dialogueOptionBox); }) // Otherwise loop back & re-spawn
            };
            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);

            dialogueOptionBox.TakeControl(battleController, this, new Action[] { () => SetupUnallocatedLootMessage(enemyName, inventoryItem, battleOutcome) });
        }

        private void SetupExitMessage(BattleOutcome battleOutcome)
        {
            string exitMessage = battleOutcome switch
            {
                BattleOutcome.Won => localizedMessageBattleCompleteWon.GetSafeLocalizedString(),
                BattleOutcome.Lost => localizedMessageBattleCompleteLost.GetSafeLocalizedString(),
                BattleOutcome.Ran => localizedMessageBattleCompleteRan.GetSafeLocalizedString(),
                _ => ""
            };

            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);
            dialogueBox.AddText(exitMessage);
            dialogueBox.TakeControl(battleController, this, new Action[] { () => { busyWithSerialAction = false; battleController.SetBattleState(BattleState.Complete, battleOutcome); } });

            battleController.SetBattleState(BattleState.Outro, battleOutcome);
        }
        #endregion

        #region Interfaces
        public void HandleDisableCallback(IUIBoxCallbackReceiver callbackReceiver, Action action)
        {
            action?.Invoke();
        }
        #endregion
    }
}
