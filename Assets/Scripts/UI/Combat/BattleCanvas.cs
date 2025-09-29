using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System;
using Frankie.Utils.UI;
using Frankie.Speech.UI;
using Frankie.Stats;
using Frankie.Inventory;
using Frankie.Inventory.UI;
using Frankie.Utils;

namespace Frankie.Combat.UI
{
    public class BattleCanvas : MonoBehaviour, IUIBoxCallbackReceiver
    {
        // Tunables
        [Header("Parents")]
        [SerializeField] Canvas canvas = null;
        [SerializeField] Transform playerPanelParent = null;
        [SerializeField] Transform frontRowParent = null;
        [SerializeField] Transform backRowParent = null;
        [SerializeField] Image backgroundFill = null;
        [SerializeField] MovingBackgroundProperties defaultMovingBackgroundProperties = null;
        [SerializeField] Transform infoChooseParent = null;
        [SerializeField] CombatLog combatLog = null;
        [SerializeField] CombatOptions combatOptions = null;
        [SerializeField] SkillSelectionUI skillSelection = null;

        [Header("Prefabs")]
        [SerializeField] CharacterSlide characterSlidePrefab = null;
        [SerializeField] EnemySlide enemySlidePrefab = null;
        [SerializeField] DialogueBox dialogueBoxPrefab = null;
        [SerializeField] DialogueOptionBox dialogueOptionBoxPrefab = null;
        [SerializeField] InventorySwapBox inventorySwapBoxPrefab = null;

        [Header("Messages")]
        [Tooltip("Include {0} for enemy name")][SerializeField] string messageEncounterSingle = "You have encountered {0}.";
        [Tooltip("Include {0} for enemy name")][SerializeField] string messageEncounterMultiple = "You have encountered {0} and its cohort.";
        [Tooltip("Called after encounter message")] [SerializeField] string messageEncounterPreHype = "What do you want to do?";
        [Tooltip("Include {0} for experience value")][SerializeField] string messageGainedExperience = "Your party has gained {0} experience.";
        [Tooltip("Include {0} for character name, {1} for level")][SerializeField] string messageCharacterLevelUp = "{0} has leveled up to level {1}!";
        [Tooltip("Include {0} for stat name, {1} for value")] [SerializeField] string messageCharacterStatGained = "{0} has increased by {1}.";
        [Tooltip("Include {0} for leader character name")] [SerializeField] string messageGainedLoot = "Wait, {0} found something.";
        [Tooltip("Include {0} for enemy name, {1} for item name")] [SerializeField] string messageEnemyDroppedLoot = "{0} dropped {1}, and you stashed it in your knapsack.";
        [Tooltip("Include {0} for enemy name, {1} for item name")] [SerializeField] string messageEnemyDroppedLootNoRoom = "{0} dropped {1}, but you don't have any room.  Do you want to throw something out?";
        [SerializeField] string optionChuckItemAffirmative = "Yeah";
        [SerializeField] string optionChuckItemNegative = "Nah";
        [Tooltip("Include {0} for item name")] [SerializeField] string messageConfirmThrowOut = "Are you sure you want to abandon {0}?";
        [SerializeField] string messageBattleCompleteWon = "You Won!  Congratulations!";
        [SerializeField] string messageBattleCompleteLost = "You lost.  Whoops!";
        [SerializeField] string messageBattleCompleteRan = "You ran away.";

        // State
        Dictionary<BattleEntity, EnemySlide> enemySlideLookup = new Dictionary<BattleEntity, EnemySlide>();
        BattleState lastBattleState = BattleState.Inactive;
        Queue<Action> queuedUISequences = new Queue<Action>();
        List<CharacterLevelUpSheetPair> queuedLevelUps = new List<CharacterLevelUpSheetPair>();
        bool firstCharacterToggle = false;
        bool busyWithSerialAction = false;
        bool outroQueued = false;

        // Cached References
        PartyCombatConduit partyCombatConduit = null;
        BattleController battleController = null;
        BattleRewards battleRewards = null;

        // Data Structures
        public struct CharacterLevelUpSheetPair
        {
            public BaseStats baseStats;
            public int level;
            public List<Tuple<string, int>> statNameValuePairs;
        }

        #region UnityMethods
        private void Awake()
        {
            partyCombatConduit = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PartyCombatConduit>();
            battleController = GameObject.FindGameObjectWithTag("BattleController")?.GetComponent<BattleController>();
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
        public void Setup(BattleStateChangedEvent battleStateChangedEvent)
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
            else if (state == BattleState.Outro || state == BattleState.PreOutro)
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

        public DialogueBox SetupRunFailureMessage(IUIBoxCallbackReceiver callbackReceiver, Action[] actions)
        {
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);
            dialogueBox.AddText("Failed to run away.");
            dialogueBox.TakeControl(battleController, callbackReceiver, actions);

            return dialogueBox;
        }

        public EnemySlide GetEnemySlide(BattleEntity combatParticipant)
        {
            if (!enemySlideLookup.ContainsKey(combatParticipant)) { return null; }

            return enemySlideLookup[combatParticipant];
        }

        public Canvas GetCanvas()
        {
            return canvas;
        }
        #endregion

        #region PrivateInitialization
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
            foreach (Transform child in infoChooseParent)
            {
                if (child.gameObject != combatOptions || child.gameObject != skillSelection) { continue; }
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
            // Note:  busyyWithSerialAction is reset on resolution of sub-menu calls
            // -- See SetupInventorySwapBox && SetupConfirmThrowOutItemMessage

            if (battleOutcome != BattleOutcome.Won) { busyWithSerialAction = false; return; }
            if (!battleRewards.HasUnallocatedLootCart()) { busyWithSerialAction = false; return; }

            DialogueOptionBox dialogueOptionBox = Instantiate(dialogueOptionBoxPrefab, infoChooseParent);
            dialogueOptionBox.Setup(string.Format(messageEnemyDroppedLootNoRoom, enemyName, inventoryItem.GetDisplayName()));

            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            choiceActionPairs.Add(new ChoiceActionPair(optionChuckItemAffirmative, () => { SetupInventorySwapBox(enemyName, inventoryItem, battleOutcome); Destroy(dialogueOptionBox.gameObject); } ));
            choiceActionPairs.Add(new ChoiceActionPair(optionChuckItemNegative, () => { SetupConfirmThrowOutItemMessage(enemyName, inventoryItem, battleOutcome); Destroy(dialogueOptionBox.gameObject); } ));
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

            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            choiceActionPairs.Add(new ChoiceActionPair(optionChuckItemAffirmative, () => { dialogueOptionBox.ClearDisableCallbacks(); busyWithSerialAction = false; Destroy(dialogueOptionBox); })); // Exit and close out serial action
            choiceActionPairs.Add(new ChoiceActionPair(optionChuckItemNegative, () => { Destroy(dialogueOptionBox); })); // Otherwise loop back & re-spawn
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
