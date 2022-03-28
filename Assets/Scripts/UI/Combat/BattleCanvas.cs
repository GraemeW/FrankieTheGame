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
        Dictionary<CombatParticipant, EnemySlide> enemySlideLookup = new Dictionary<CombatParticipant, EnemySlide>();
        BattleState lastBattleState = BattleState.Inactive;
        Queue<Action> queuedUISequences = new Queue<Action>();
        List<CharacterLevelUpSheetPair> queuedLevelUps = new List<CharacterLevelUpSheetPair>();
        bool busyWithSerialAction = false;
        bool outroQueued = false;

        // Cached References
        PartyCombatConduit partyCombatConduit = null;
        BattleController battleController = null;

        // Data Structures
        public struct CharacterLevelUpSheetPair
        {
            public CombatParticipant character;
            public int level;
            public List<Tuple<string, int>> statNameValuePairs;
        }

        #region UnityMethods
        private void Awake()
        {
            battleController = GameObject.FindGameObjectWithTag("BattleController")?.GetComponent<BattleController>();
            partyCombatConduit = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PartyCombatConduit>();

            combatOptions.Setup(battleController, this, partyCombatConduit);
            combatOptions.TakeControl(battleController, this, null);

            ClearBattleCanvas();
        }

        private void OnEnable()
        {
            battleController.battleStateChanged += Setup;
        }

        private void OnDisable()
        {
            battleController.battleStateChanged -= Setup;
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
        public void Setup(BattleState state)
        {
            if (state == lastBattleState) { return; } // Only act on state changes
            lastBattleState = state;

            if (state == BattleState.Intro)
            {
                SetupPlayerCharacters(battleController.GetCharacters());
                SetupEnemies(battleController.GetEnemies());
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
                queuedUISequences.Enqueue(SetupExperienceMessage);
                SetupAllLootMessages(); // Parent function to queue a number of additional loot messages into UI sequences
                queuedUISequences.Enqueue(SetupExitMessage);
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

        public EnemySlide GetEnemySlide(CombatParticipant combatParticipant)
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

        private void SetupPlayerCharacters(IEnumerable characters)
        {
            bool firstCharacterToggle = false;
            foreach (CombatParticipant character in characters)
            {
                CharacterSlide characterSlide = Instantiate(characterSlidePrefab, playerPanelParent);
                characterSlide.SetCombatParticipant(character);
                combatLog.AddCombatListener(character);

                if (!firstCharacterToggle)
                {
                    characterSlide.GetComponent<Button>().Select();
                    battleController.SetSelectedCharacter(character);
                    firstCharacterToggle = true;
                }
                character.characterLevelUp += HandleLevelUp; 
            }
        }

        private void SetupEnemies(IEnumerable enemies)
        {
            foreach (CombatParticipant enemy in enemies)
            {
                Transform parentSpawn = (frontRowParent.childCount - 1 > backRowParent.childCount) ? backRowParent : frontRowParent;
                EnemySlide enemySlide = Instantiate(enemySlidePrefab, parentSpawn);
                enemySlide.SetCombatParticipant(enemy);
                combatLog.AddCombatListener(enemy);

                enemySlideLookup[enemy] = enemySlide;
            }
        }

        private void SetupBackgroundFill(List<CombatParticipant> enemies)
        {
            int enemyIndex = UnityEngine.Random.Range(0, enemies.Count);
            MovingBackgroundProperties movingBackgroundProperties = enemies[enemyIndex].GetMovingBackgroundProperties();
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

        private void HandleLevelUp(CombatParticipant character, int level, List<Tuple<string, int>> statNameValuePairs)
        {
            CharacterLevelUpSheetPair characterLevelUpSheetPair = new CharacterLevelUpSheetPair
            {
                character = character,
                level = level,
                statNameValuePairs = statNameValuePairs
            };
            queuedLevelUps.Add(characterLevelUpSheetPair);
        }

        private void SetupEntryMessage(List<CombatParticipant> enemies)
        {
            CombatParticipant enemy = enemies.FirstOrDefault();
            string entryMessage = (enemies.Count > 1) ? string.Format(messageEncounterMultiple, enemy.GetCombatName()) : string.Format(messageEncounterSingle, enemy.GetCombatName());

            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);
            dialogueBox.AddText(entryMessage);
            dialogueBox.AddPageBreak();
            dialogueBox.AddText(messageEncounterPreHype);
            dialogueBox.TakeControl(battleController, this, new Action[] { () => battleController.SetBattleState(BattleState.PreCombat) });
        }

        private void SetupExperienceMessage()
        {
            if (battleController.GetBattleOutcome() != BattleOutcome.Won) { busyWithSerialAction = false; return; }

            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);
            dialogueBox.AddText(string.Format(messageGainedExperience, Mathf.RoundToInt(battleController.GetBattleExperienceReward()).ToString()));

            foreach (CharacterLevelUpSheetPair characterLevelUpSheetPair in queuedLevelUps)
            {
                dialogueBox.AddPageBreak();

                dialogueBox.AddText(string.Format(messageCharacterLevelUp, characterLevelUpSheetPair.character.GetCombatName(), characterLevelUpSheetPair.level.ToString()));
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

                characterLevelUpSheetPair.character.characterLevelUp -= HandleLevelUp;
                    // Unsubscribe to messages -- not the cleanest location, but the only one available
            }

            dialogueBox.TakeControl(battleController, this, new Action[] { () => busyWithSerialAction = false });
        }

        private void SetupAllLootMessages()
        {
            queuedUISequences.Enqueue(SetupPreLootMessage);
            queuedUISequences.Enqueue(SetupAllocatedLootMessage);
            foreach (Tuple<string, InventoryItem> enemyItemPair in battleController.GetUnallocatedLootCart())
            {
                queuedUISequences.Enqueue(() => SetupUnallocatedLootMessage(enemyItemPair.Item1, enemyItemPair.Item2));
            }
        }

        private void SetupPreLootMessage()
        {
            if (battleController.GetBattleOutcome() != BattleOutcome.Won) { busyWithSerialAction = false; return; }
            if (!battleController.HasLootCart()) { busyWithSerialAction = false; return; }

            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);
            dialogueBox.AddText(string.Format(messageGainedLoot, partyCombatConduit.GetPartyLeaderName()));

            dialogueBox.TakeControl(battleController, this, new Action[] { () => busyWithSerialAction = false });
        }

        private void SetupAllocatedLootMessage()
        {
            // Handling items that were easily placed in inventory
            if (battleController.GetBattleOutcome() != BattleOutcome.Won) { busyWithSerialAction = false; return; }
            if (!battleController.HasAllocatedLootCart()) { busyWithSerialAction = false; return; }
            
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);

            foreach (Tuple<string, InventoryItem> enemyItemPair in battleController.GetAllocatedLootCart())
            {
                dialogueBox.AddText(string.Format(messageEnemyDroppedLoot, enemyItemPair.Item1, enemyItemPair.Item2.GetDisplayName()));
                dialogueBox.AddPageBreak();
            }

            dialogueBox.TakeControl(battleController, this, new Action[] { () => busyWithSerialAction = false });
        }

        private void SetupUnallocatedLootMessage(string enemyName, InventoryItem inventoryItem)
        {
            // Handling items that cannot be placed in inventory due to inventory full
            // Note:  busyyWithSerialAction is reset on resolution of sub-menu calls
            // -- See SetupInventorySwapBox && SetupConfirmThrowOutItemMessage

            if (battleController.GetBattleOutcome() != BattleOutcome.Won) { busyWithSerialAction = false; return; }
            if (!battleController.HasUnallocatedLootCart()) { busyWithSerialAction = false; return; }

            DialogueOptionBox dialogueOptionBox = Instantiate(dialogueOptionBoxPrefab, infoChooseParent);
            dialogueOptionBox.Setup(string.Format(messageEnemyDroppedLootNoRoom, enemyName, inventoryItem.GetDisplayName()));

            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            choiceActionPairs.Add(new ChoiceActionPair(optionChuckItemAffirmative, () => { SetupInventorySwapBox(enemyName, inventoryItem); Destroy(dialogueOptionBox.gameObject); } ));
            choiceActionPairs.Add(new ChoiceActionPair(optionChuckItemNegative, () => { SetupConfirmThrowOutItemMessage(enemyName, inventoryItem); Destroy(dialogueOptionBox.gameObject); } ));
            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);

            dialogueOptionBox.ClearDisableCallbacksOnChoose(true); // Clear window re-spawn (see below) on successful choice selection
            dialogueOptionBox.TakeControl(battleController, this, new Action[] { () => SetupUnallocatedLootMessage(enemyName, inventoryItem) }); // If user tabs out of this window, re-spawn it (avoid lost loot)
        }

        private void SetupInventorySwapBox(string enemyName, InventoryItem inventoryItem)
        {
            InventorySwapBox inventorySwapBox = Instantiate(inventorySwapBoxPrefab, infoChooseParent);
            inventorySwapBox.Setup(battleController, partyCombatConduit, inventoryItem, () => { inventorySwapBox.ClearDisableCallbacks(); busyWithSerialAction = false; });
                // Inventory box destruction handled by swap box on successful swap

            inventorySwapBox.TakeControl(battleController, this, new Action[] { () => SetupUnallocatedLootMessage(enemyName, inventoryItem) });
            // If user tabs out of this window, re-spawn it (avoid lost loot)
        }

        private void SetupConfirmThrowOutItemMessage(string enemyName, InventoryItem inventoryItem)
        {
            DialogueOptionBox dialogueOptionBox = Instantiate(dialogueOptionBoxPrefab, infoChooseParent);
            dialogueOptionBox.Setup(string.Format(messageConfirmThrowOut, inventoryItem.GetDisplayName()));

            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            choiceActionPairs.Add(new ChoiceActionPair(optionChuckItemAffirmative, () => { dialogueOptionBox.ClearDisableCallbacks(); busyWithSerialAction = false; Destroy(dialogueOptionBox); })); // Exit and close out serial action
            choiceActionPairs.Add(new ChoiceActionPair(optionChuckItemNegative, () => { Destroy(dialogueOptionBox); })); // Otherwise loop back & re-spawn
            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);

            dialogueOptionBox.TakeControl(battleController, this, new Action[] { () => SetupUnallocatedLootMessage(enemyName, inventoryItem) });
        }

        private void SetupExitMessage()
        {
            string exitMessage = "";
            if (battleController.GetBattleOutcome() == BattleOutcome.Won)
            {
                exitMessage = messageBattleCompleteWon;
            }
            else if (battleController.GetBattleOutcome() == BattleOutcome.Lost)
            {
                exitMessage = messageBattleCompleteLost;
            }
            else if (battleController.GetBattleOutcome() == BattleOutcome.Ran)
            {
                exitMessage = messageBattleCompleteRan;
            }

            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, infoChooseParent);
            dialogueBox.AddText(exitMessage);
            dialogueBox.TakeControl(battleController, this, new Action[] { () => { busyWithSerialAction = false; battleController.SetBattleState(BattleState.Complete); } });

            battleController.SetBattleState(BattleState.Outro);
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
