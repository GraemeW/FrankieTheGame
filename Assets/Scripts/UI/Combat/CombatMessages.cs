using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Localization;
using Frankie.Speech.UI;
using Frankie.Stats;
using Frankie.Utils.Localization;

namespace Frankie.Combat.UI
{
    public sealed class CombatMessages : MonoBehaviour, ILocalizable
    {
        // Handles spawning dialogue boxes for:
        //  - encounter intro
        //  - item-in-use prompt
        //  - run failure
        //  - outro (run one at a time through a sequence queue)
        // Note:  Listens to the battle directly;  Setup(...) hands it the battle's dependencies

        // Tunables
        [Header("Hook-Ups")]
        [SerializeField] private Transform messageParent;
        [SerializeField] private DialogueBox dialogueBoxPrefab;
        [Header("Encounter Messages")]
        [Header("{0} for enemy name")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageEncounterSingle;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageEncounterMultiple;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageEncounterPreHype;
        [Header("Combat Messages")]
        [Header("Include {0} for item")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageItemToBeUsed;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageFailedToRun;
        [Header("Post-Combat Messages")]
        [Header("Include {0} for experience value")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageGainedExperience;
        [Header("Include {0} for character name, {1} for level")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageCharacterLevelUp;
        [Header("Include {0} for stat name, {1} for value")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageCharacterStatGained;
        [Header("Loot Messages")]
        [SerializeField] private BattleLootMessages lootMessages = new();
        [Header("Exit Combat Messages")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageBattleCompleteWon;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageBattleCompleteLost;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageBattleCompleteRan;

        // Const Tunables
        private const int _statGainsPerPage = 3;

        // State
        private DialogueBox itemMessageBox;
        private readonly Queue<Action<Action>> queuedSequences = new(); // Each step is handed the callback that releases the queue
        private bool isSequenceRunning = false;
        private bool isOutroQueued = false;
        private readonly List<LevelUpEntry> levelUps = new();
        private readonly List<BaseStats> trackedCharacters = new();

        // Cached References
        private BattleController battleController;
        private BattleRewards battleRewards;

        // Data Structures
        private struct LevelUpEntry
        {
            public BaseStats baseStats;
            public int level;
            public List<(string statName, int gain)> statGains;
        }

        #region UnityMethods
        private void OnEnable()
        {
            BattleEventBus<BattleStagingEvent>.SubscribeToEvent(HandleBattleStagingEvent);
            BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(HandleBattleStateChangedEvent);
            BattleEventBus<BattleEntityAddedEvent>.SubscribeToEvent(HandleBattleEntityAddedEvent);
            BattleEventBus<BattleActionArmedEvent>.SubscribeToEvent(HandleBattleActionArmedEvent);
        }

        private void OnDisable()
        {
            BattleEventBus<BattleStagingEvent>.UnsubscribeFromEvent(HandleBattleStagingEvent);
            BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(HandleBattleStateChangedEvent);
            BattleEventBus<BattleEntityAddedEvent>.UnsubscribeFromEvent(HandleBattleEntityAddedEvent);
            BattleEventBus<BattleActionArmedEvent>.UnsubscribeFromEvent(HandleBattleActionArmedEvent);
        }

        private void OnDestroy()
        {
            foreach (BaseStats trackedCharacter in trackedCharacters)
            {
                if (trackedCharacter != null) { trackedCharacter.onLevelUp -= HandleLevelUp; }
            }
            trackedCharacters.Clear();
        }

        private void Update()
        {
            SetupNextMessageInQueue();
        }
        #endregion

        #region PublicMethods
        public void Setup(BattleController setBattleController, PartyCombatConduit partyCombatConduit)
        {
            battleController = setBattleController;
            battleRewards = setBattleController.GetBattleRewards();
            lootMessages.Setup(setBattleController, partyCombatConduit, dialogueBoxPrefab, messageParent);
        }

        public void ShowRunFailureMessage(Action onDismissed)
        {
            DialogueBox dialogueBox = SpawnDialogueBox();
            dialogueBox.AddText(localizedMessageFailedToRun.GetSafeLocalizedString());
            battleController.AddInputReceiver(dialogueBox, onDismissed);
        }
        #endregion

        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            var localizationEntries = new List<TableEntryReference>
            {
                localizedMessageEncounterSingle.TableEntryReference,
                localizedMessageEncounterMultiple.TableEntryReference,
                localizedMessageEncounterPreHype.TableEntryReference,
                localizedMessageItemToBeUsed.TableEntryReference,
                localizedMessageFailedToRun.TableEntryReference,
                localizedMessageGainedExperience.TableEntryReference,
                localizedMessageCharacterLevelUp.TableEntryReference,
                localizedMessageCharacterStatGained.TableEntryReference,
                localizedMessageBattleCompleteWon.TableEntryReference,
                localizedMessageBattleCompleteLost.TableEntryReference,
                localizedMessageBattleCompleteRan.TableEntryReference
            };
            localizationEntries.AddRange(lootMessages.GetLocalizationEntries());
            return localizationEntries;
        }
        #endregion

        #region EventHandlers
        private void HandleBattleStagingEvent(BattleStagingEvent battleStagingEvent)
        {
            if (battleStagingEvent.battleStagingType != BattleStagingType.BattleControllerPrimed || !battleStagingEvent.optionalParametersSet) { return; }
            ShowEncounterMessage(battleStagingEvent.GetEnemyEntities());
        }

        private void HandleBattleStateChangedEvent(BattleStateChangedEvent battleStateChangedEvent)
        {
            if (battleStateChangedEvent.battleState is not (BattleState.Outro or BattleState.Rewards) || isOutroQueued) { return; }
            isOutroQueued = true;

            BattleOutcome battleOutcome = battleStateChangedEvent.battleOutcome;
            queuedSequences.Enqueue(onComplete => ShowExperienceMessage(battleOutcome, onComplete));
            foreach (Action<Action> lootSequence in lootMessages.GetSequences(battleOutcome)) { queuedSequences.Enqueue(lootSequence); }
            queuedSequences.Enqueue(onComplete => ShowExitMessage(battleOutcome, onComplete));
        }

        // Note:  Level-ups are collected as they happen and reported with the experience message (no level-ups for assist characters)
        private void HandleBattleEntityAddedEvent(BattleEntityAddedEvent battleEntityAddedEvent)
        {
            BattleEntity battleEntity = battleEntityAddedEvent.battleEntity;
            if (battleEntity == null || !battleEntity.isCharacter || battleEntity.isAssistCharacter || battleEntity.combatParticipant == null) { return; }
            if (!battleEntity.combatParticipant.TryGetComponent(out BaseStats baseStats) || trackedCharacters.Contains(baseStats)) { return; }

            baseStats.onLevelUp += HandleLevelUp;
            trackedCharacters.Add(baseStats);
        }

        private void HandleLevelUp(BaseStats baseStats, int level, Dictionary<Stat, float> levelUpSheet)
        {
            levelUps.Add(new LevelUpEntry
            {
                baseStats = baseStats,
                level = level,
                statGains = levelUpSheet.Select(entry => (LocalizationNames.GetLocalizedName(entry.Key), Mathf.RoundToInt(entry.Value))).ToList()
            });
        }

        private void HandleBattleActionArmedEvent(BattleActionArmedEvent battleActionArmedEvent)
        {
            // Note:  One message at most - disarm still carries the action that was armed
            if (itemMessageBox != null) { Destroy(itemMessageBox.gameObject); }
            itemMessageBox = null;

            IBattleActionSuper battleActionSuper = battleActionArmedEvent.battleActionSuper;
            if (!battleActionArmedEvent.isArmed || battleActionSuper == null || !battleActionSuper.IsItem()) { return; }

            itemMessageBox = SpawnDialogueBox();
            itemMessageBox.AddText(string.Format(localizedMessageItemToBeUsed.GetSafeLocalizedString(), battleActionSuper.GetName()));
            itemMessageBox.SetActiveInput(false);
        }
        #endregion

        #region PrivateMethods
        private DialogueBox SpawnDialogueBox() => Instantiate(dialogueBoxPrefab, messageParent);

        private void SetupNextMessageInQueue()
        {
            if (isSequenceRunning || queuedSequences.Count == 0) { return; }
            
            Action<Action> nextUp = queuedSequences.Dequeue();
            if (nextUp == null) { return; }
            
            isSequenceRunning = true;
            nextUp.Invoke(() => isSequenceRunning = false);
        }
        
        private void ShowEncounterMessage(IList<BattleEntity> enemies)
        {
            BattleEntity enemy = enemies?.FirstOrDefault(battleEntity => battleEntity != null && battleEntity.combatParticipant != null);
            if (enemy == null) { return; }

            LocalizedString localizedEncounterMessage = enemies.Count > 1 ? localizedMessageEncounterMultiple : localizedMessageEncounterSingle;
            DialogueBox dialogueBox = SpawnDialogueBox();
            dialogueBox.AddText(string.Format(localizedEncounterMessage.GetSafeLocalizedString(), enemy.combatParticipant.GetCombatName()));
            dialogueBox.AddPageBreak();
            dialogueBox.AddText(localizedMessageEncounterPreHype.GetSafeLocalizedString());
            battleController.AddInputReceiver(dialogueBox, () => battleController.SetBattleState(BattleState.PreCombat, BattleOutcome.Undetermined));
        }

        private void ShowExperienceMessage(BattleOutcome battleOutcome, Action onComplete)
        {
            if (battleOutcome != BattleOutcome.Won) { onComplete(); return; }

            DialogueBox dialogueBox = SpawnDialogueBox();
            dialogueBox.AddText(string.Format(localizedMessageGainedExperience.GetSafeLocalizedString(), Mathf.RoundToInt(battleRewards.GetBattleExperienceReward()).ToString(CultureInfo.InvariantCulture)));

            foreach (LevelUpEntry levelUp in levelUps)
            {
                dialogueBox.AddPageBreak();
                dialogueBox.AddText(string.Format(localizedMessageCharacterLevelUp.GetSafeLocalizedString(), CharacterProperties.GetCharacterDisplayName(levelUp.baseStats), levelUp.level.ToString(CultureInfo.InvariantCulture)));

                int statGainsOnPage = 0;
                foreach ((string statName, int gain) in levelUp.statGains)
                {
                    if (gain == 0) { continue; }
                    if (statGainsOnPage >= _statGainsPerPage)
                    {
                        dialogueBox.AddPageBreak();
                        statGainsOnPage = 0;
                    }

                    dialogueBox.AddText(string.Format(localizedMessageCharacterStatGained.GetSafeLocalizedString(), statName, gain.ToString(CultureInfo.InvariantCulture)));
                    statGainsOnPage++;
                }
            }
            battleController.AddInputReceiver(dialogueBox, onComplete);
        }

        private void ShowExitMessage(BattleOutcome battleOutcome, Action onComplete)
        {
            string exitMessage = battleOutcome switch
            {
                BattleOutcome.Won => localizedMessageBattleCompleteWon.GetSafeLocalizedString(),
                BattleOutcome.Lost => localizedMessageBattleCompleteLost.GetSafeLocalizedString(),
                BattleOutcome.Ran => localizedMessageBattleCompleteRan.GetSafeLocalizedString(),
                _ => ""
            };

            DialogueBox dialogueBox = SpawnDialogueBox();
            dialogueBox.AddText(exitMessage);
            battleController.AddInputReceiver(dialogueBox, () =>
            {
                onComplete();
                battleController.SetBattleState(BattleState.Complete, battleOutcome);
            });
            battleController.SetBattleState(BattleState.Outro, battleOutcome);
        }
        #endregion
    }
}
