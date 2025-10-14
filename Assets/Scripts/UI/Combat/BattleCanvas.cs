using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Frankie.Utils.UI;
using Frankie.Speech.UI;
using Frankie.Stats;
using Frankie.Inventory;
using Frankie.Inventory.UI;
using Frankie.Utils;
using Frankie.Core;

namespace Frankie.Combat.UI
{
    public class BattleCanvas : MonoBehaviour, IUIBoxCallbackReceiver
    {
        // Tunables
        [Header("Parents")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Transform playerPanelParent;
        [SerializeField] private Transform frontRowParent;
        [SerializeField] private Transform backRowParent;
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

        [Header("Messages")]
        [Tooltip("Include {0} for enemy name")][SerializeField] private string messageEncounterSingle = "You have encountered {0}.";
        [Tooltip("Include {0} for enemy name")][SerializeField] private string messageEncounterMultiple = "You have encountered {0} and its cohort.";
        [Tooltip("Called after encounter message")] [SerializeField] private string messageEncounterPreHype = "What do you want to do?";
        [Tooltip("Include {0} for experience value")][SerializeField] private string messageGainedExperience = "Your party has gained {0} experience.";
        [Tooltip("Include {0} for character name, {1} for level")][SerializeField] private string messageCharacterLevelUp = "{0} has leveled up to level {1}!";
        [Tooltip("Include {0} for stat name, {1} for value")] [SerializeField] private string messageCharacterStatGained = "{0} has increased by {1}.";
        [Tooltip("Include {0} for leader character name")] [SerializeField] private string messageGainedLoot = "Wait, {0} found something.";
        [Tooltip("Include {0} for enemy name, {1} for item name")] [SerializeField] private string messageEnemyDroppedLoot = "{0} dropped {1}, and you stashed it in your knapsack.";
        [Tooltip("Include {0} for enemy name, {1} for item name")] [SerializeField] private string messageEnemyDroppedLootNoRoom = "{0} dropped {1}, but you don't have any room.  Do you want to throw something out?";
        [SerializeField] private string optionChuckItemAffirmative = "Yeah";
        [SerializeField] private string optionChuckItemNegative = "Nah";
        [Tooltip("Include {0} for item name")] [SerializeField] private string messageConfirmThrowOut = "Are you sure you want to abandon {0}?";
        [SerializeField] private string messageBattleCompleteWon = "You Won!  Congratulations!";
        [SerializeField] private string messageBattleCompleteLost = "You lost.  Whoops!";
        [SerializeField] private string messageBattleCompleteRan = "You ran away.";

        // State
        private readonly Dictionary<BattleEntity, EnemySlide> enemySlideLookup = new Dictionary<BattleEntity, EnemySlide>();
        private BattleState lastBattleState = BattleState.Inactive;
        private readonly Queue<Action> queuedUISequences = new Queue<Action>();
        private readonly List<CharacterLevelUpSheetPair> queuedLevelUps = new List<CharacterLevelUpSheetPair>();
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
            combatOptions.Setup(battleController, this, partyCombatConduit);
            combatOptions.TakeControl(battleController, this, null);

            ClearBattleCanvas();
        }

        private void OnEnable()
        {
            BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(Setup);
            BattleEventBus<BattleEntityAddedEvent>.SubscribeToEvent(SetupBattleEntity);
        }

        private void OnDisable()
        {
            BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(Setup);
            BattleEventBus<BattleEntityAddedEvent>.UnsubscribeFromEvent(SetupBattleEntity);
        }

        private void OnDestroy()
        {
            ClearBattleCanvas();
        }

        private void Update()
        {
            if (!busyWithSerialAction && queuedUISequences.Count != 0)
            {
                Action nextUISequence = queuedUISequences.Dequeue();
                StartSerialAction(nextUISequence);
            }
        }
        #endregion

        #region PublicMethods
        public DialogueBox SetupRunFailureMessage(IUIBoxCallbackReceiver callbackReceiver, Action[] actions)
        {
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);
            dialogueBox.AddText("Failed to run away.");
            dialogueBox.TakeControl(battleController, callbackReceiver, actions);

            return dialogueBox;
        }

        public EnemySlide GetEnemySlide(BattleEntity combatParticipant)
        {
            return enemySlideLookup.GetValueOrDefault(combatParticipant);
        }

        public Canvas GetCanvas()
        {
            return canvas;
        }
        #endregion

        #region PrivateInitialization
        private void Setup(BattleStateChangedEvent battleStateChangedEvent)
        {
            BattleState state = battleStateChangedEvent.battleState;
            BattleOutcome battleOutcome = battleStateChangedEvent.battleOutcome;

            if (state == lastBattleState) { return; } // Only act on state changes
            lastBattleState = state;

            if (state == BattleState.Intro)
            {
                SetupBackgroundFill(battleController.GetEnemies());
                SetupEntryMessage(battleController.GetEnemies());
            }
            else if (state == BattleState.PreCombat)
            {
                skillSelection.gameObject.SetActive(false);
                combatOptions.SetCombatOptions(true);
            }
            else if (state == BattleState.Combat)
            {
                combatLog.AddCombatLogText("  Combat Started . . . ");
                combatLog.gameObject.SetActive(true);
                skillSelection.gameObject.SetActive(true);
            }
            else if (state == BattleState.Outro || state == BattleState.Rewards)
            {
                if (outroQueued) { return; }

                combatLog.gameObject.SetActive(false);
                skillSelection.gameObject.SetActive(false);
                queuedUISequences.Enqueue(() => SetupExperienceMessage(battleOutcome));
                SetupAllLootMessages(battleOutcome); // Parent function to queue a number of additional loot messages into UI sequences
                queuedUISequences.Enqueue(() => SetupExitMessage(battleOutcome));
                outroQueued = true;
            }
        }
        
        private void ClearBattleCanvas()
        {
            foreach (Transform child in playerPanelParent)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in frontRowParent)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in backRowParent)
            {
                Destroy(child.gameObject);
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
            Transform parentSpawn = (battleEntity.row == 0) ? frontRowParent : backRowParent;
            EnemySlide enemySlide = Instantiate(enemySlidePrefab, parentSpawn);
            enemySlide.SetBattleEntity(battleEntity);
            combatLog.AddCombatListener(battleEntity.combatParticipant);

            enemySlideLookup[battleEntity] = enemySlide;
        }

        private void SetupCharacter(BattleEntity character)
        {
            CharacterSlide characterSlide = Instantiate(characterSlidePrefab, playerPanelParent);
            characterSlide.SetBattleEntity(character);
            combatLog.AddCombatListener(character.combatParticipant);

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
            combatLog.AddCombatListener(assistCharacter.combatParticipant);
        }

        private void SetupBackgroundFill(List<BattleEntity> enemies)
        {
            int enemyIndex = UnityEngine.Random.Range(0, enemies.Count);
            MovingBackgroundProperties movingBackgroundProperties = enemies[enemyIndex].combatParticipant.GetMovingBackgroundProperties();
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
        #endregion

        #region PrivateUtility
        private void StartSerialAction(Action action)
        {
            // !! It is the responsibility of the called action to reset busyWithSerialAction toggle !!
            if (action != null)
            {
                busyWithSerialAction = true;
                action.Invoke();
            }
        }

        private void HandleLevelUp(BaseStats baseStats, int level, Dictionary<Stat, float> levelUpSheet)
        {
            List<Tuple<string, int>> statNameValuePairs = new List<Tuple<string, int>>();
            foreach (KeyValuePair<Stat, float> entry in levelUpSheet)
            {
                Tuple<string, int> statNameValuePair = new Tuple<string, int>(entry.Key.ToString(), Mathf.RoundToInt(entry.Value));
                statNameValuePairs.Add(statNameValuePair);
            }

            CharacterLevelUpSheetPair characterLevelUpSheetPair = new CharacterLevelUpSheetPair
            {
                baseStats = baseStats,
                level = level,
                statNameValuePairs = statNameValuePairs
            };
            queuedLevelUps.Add(characterLevelUpSheetPair);
        }

        private void SetupEntryMessage(List<BattleEntity> enemies)
        {
            BattleEntity enemy = enemies.FirstOrDefault();
            if (enemy == null) { return; }
            
            string entryMessage = (enemies.Count > 1) ? string.Format(messageEncounterMultiple, enemy.combatParticipant.GetCombatName()) : string.Format(messageEncounterSingle, enemy.combatParticipant.GetCombatName());

            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);
            dialogueBox.AddText(entryMessage);
            dialogueBox.AddPageBreak();
            dialogueBox.AddText(messageEncounterPreHype);
            dialogueBox.TakeControl(battleController, this, new Action[] { () => battleController.SetBattleState(BattleState.PreCombat, BattleOutcome.Undetermined) });
        }

        private void SetupExperienceMessage(BattleOutcome battleOutcome)
        {
            if (battleOutcome != BattleOutcome.Won) { busyWithSerialAction = false; return; }

            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);
            dialogueBox.AddText(string.Format(messageGainedExperience, Mathf.RoundToInt(battleRewards.GetBattleExperienceReward()).ToString()));

            foreach (CharacterLevelUpSheetPair characterLevelUpSheetPair in queuedLevelUps)
            {
                dialogueBox.AddPageBreak();

                dialogueBox.AddText(string.Format(messageCharacterLevelUp, characterLevelUpSheetPair.baseStats.GetCharacterProperties().GetCharacterNamePretty(), characterLevelUpSheetPair.level.ToString()));
                int pageClearReset = 0;

                foreach (Tuple<string, int> statNameValuePair in characterLevelUpSheetPair.statNameValuePairs)
                {
                    if (pageClearReset > 2)
                    {
                        dialogueBox.AddPageBreak();
                        pageClearReset = 0;
                    }

                    dialogueBox.AddText(string.Format(messageCharacterStatGained, statNameValuePair.Item1, statNameValuePair.Item2.ToString()));
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
            if (battleOutcome != BattleOutcome.Won) { busyWithSerialAction = false; return; }
            if (!battleRewards.HasLootCart()) { busyWithSerialAction = false; return; }

            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);
            dialogueBox.AddText(string.Format(messageGainedLoot, partyCombatConduit.GetPartyLeaderName()));

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
                dialogueBox.AddText(string.Format(messageEnemyDroppedLoot, enemyItemPair.Item1, enemyItemPair.Item2.GetDisplayName()));
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
            dialogueOptionBox.Setup(string.Format(messageEnemyDroppedLootNoRoom, enemyName, inventoryItem.GetDisplayName()));

            var choiceActionPairs = new List<ChoiceActionPair>
            {
                new ChoiceActionPair(optionChuckItemAffirmative, () => { SetupInventorySwapBox(enemyName, inventoryItem, battleOutcome); Destroy(dialogueOptionBox.gameObject); } ),
                new ChoiceActionPair(optionChuckItemNegative, () => { SetupConfirmThrowOutItemMessage(enemyName, inventoryItem, battleOutcome); Destroy(dialogueOptionBox.gameObject); } )
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
            dialogueOptionBox.Setup(string.Format(messageConfirmThrowOut, inventoryItem.GetDisplayName()));

            var choiceActionPairs = new List<ChoiceActionPair>
            {
                new ChoiceActionPair(optionChuckItemAffirmative, () => { dialogueOptionBox.ClearDisableCallbacks(); busyWithSerialAction = false; Destroy(dialogueOptionBox); }), // Exit and close out serial action
                new ChoiceActionPair(optionChuckItemNegative, () => { Destroy(dialogueOptionBox); }) // Otherwise loop back & re-spawn
            };
            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);

            dialogueOptionBox.TakeControl(battleController, this, new Action[] { () => SetupUnallocatedLootMessage(enemyName, inventoryItem, battleOutcome) });
        }

        private void SetupExitMessage(BattleOutcome battleOutcome)
        {
            string exitMessage = "";
            if (battleOutcome == BattleOutcome.Won)
            {
                exitMessage = messageBattleCompleteWon;
            }
            else if (battleOutcome == BattleOutcome.Lost)
            {
                exitMessage = messageBattleCompleteLost;
            }
            else if (battleOutcome == BattleOutcome.Ran)
            {
                exitMessage = messageBattleCompleteRan;
            }

            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);
            dialogueBox.AddText(exitMessage);
            dialogueBox.TakeControl(battleController, this, new Action[] { () => { busyWithSerialAction = false; battleController.SetBattleState(BattleState.Complete, battleOutcome); } });

            battleController.SetBattleState(BattleState.Outro, battleOutcome);
        }
        #endregion

        #region Interfaces
        public void HandleDisableCallback(IUIBoxCallbackReceiver dialogueBox, Action action)
        {
            action?.Invoke();
        }
        #endregion
    }
}
