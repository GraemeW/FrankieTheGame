using System;
using UnityEngine;
using UnityEngine.UIElements;
using Frankie.Core;
using Frankie.Core.Predicates;
using Frankie.Control;
using Frankie.Sound;
using Frankie.Stats;
using Frankie.Combat;
using Frankie.Inventory;
using Frankie.Quests;
using Frankie.World;
using Frankie.ZoneManagement;

namespace Frankie.Saving.Editor
{
    public abstract class SaveableSubCardData
    {
        // Const Tunables
        private const string _statusSyncMessage = "Data Sync:  OK";
        private const string _statusDesyncMessage = "Data Desync:  Volatile";
        private static readonly Color _statusSyncColor = Color.lightGreen;
        private static readonly Color _statusDesyncColor = Color.softRed;
        protected const float smallButtonWidth = 100f;
        protected const float standardButtonWidth = 175f;
        protected const float largeButtonWidth = 250f;
        
        // State
        protected SaveableEntityCardData saveableEntityCardData { get; private set; }
        public ISaveableBase saveable { get; protected set; }
        public SaveState saveState { get; protected set; }
        // Events
        public event Action<string, SaveState> saveStateChanged;
        // ActiveState
        private bool isSaveStateSynced = true;
        // UI State
        private Box contentContainer;
        private Label syncStateLabel;

        public static SaveableSubCardData CreateTypeSpecificSubCard(ISaveableBase saveable, SaveState saveState)
        {
            return saveable switch
            {
                Mover => new MoverSaveableSubCard(saveable, saveState),
                PlayerColliderTrigger => new SimpleBoolSaveableSubCard(saveable, saveState),
                BaseStats => new BaseStatsSubCard(saveable, saveState),
                Experience => new SimpleFloatSaveableSubCard(saveable, saveState),
                CombatParticipant => new CombatParticipantSaveableSubCard(saveable, saveState),
                Party => new PartySubCard(saveable, saveState),
                PartyAssist => new PartyAssistSubCard(saveable, saveState),
                InactiveParty => new InactivePartySubCard(saveable, saveState),
                Knapsack => new KnapsackSaveableSubCard(saveable, saveState),
                Equipment => new EquipmentSaveableSubCard(saveable, saveState),
                WearablesLink => new WearablesLinkSubCard(saveable, saveState),
                Wallet => new WalletSaveableSubCard(saveable, saveState),
                QuestList => new QuestListSubCard(saveable, saveState),
                CheckBase => new SimpleBoolSaveableSubCard(saveable, saveState),
                PredicateChildToggler => new SimpleBoolSaveableSubCard(saveable, saveState),
                BackgroundMusicOverride => new SimpleBoolSaveableSubCard(saveable, saveState),
                CinematicTrigger => new SimpleBoolSaveableSubCard(saveable, saveState),
                FlickerOverlay => new SimpleBoolSaveableSubCard(saveable, saveState),
                WorldSpriteChanger => new SimpleBoolSaveableSubCard(saveable, saveState),
                WorldCashGiverTaker => new SimpleIntSaveableSubCard(saveable, saveState),
                WorldItemGiverTaker => new SimpleIntSaveableSubCard(saveable, saveState),
                Room => new SimpleBoolSaveableSubCard(saveable, saveState),
                _ => new GenericSaveableSubCard(saveable, saveState)
            };
        }
        
        public abstract void AddEditableFieldsToSubCardView(Box subCardView);
        
        public bool IsSaveStateSynced() => isSaveStateSynced;
        
        public void SetSaveableEntityCardData(SaveableEntityCardData setSaveableEntityCardData) => saveableEntityCardData = setSaveableEntityCardData;
        
        public void ResetSyncFlag() => UpdateSyncState(true);
        
        public void DrawIntoSubCardView(Box subCardView)
        {
            contentContainer = new Box();
            subCardView.Add(contentContainer);
            Redraw();
        }
        
        public void Redraw()
        {
            if (contentContainer == null) { return; }
            contentContainer.Clear();
            syncStateLabel = new Label(isSaveStateSynced ? _statusSyncMessage : _statusDesyncMessage) { style = { color = isSaveStateSynced ? _statusSyncColor : _statusDesyncColor } };
            contentContainer.Add(syncStateLabel);
            AddEditableFieldsToSubCardView(contentContainer);
        }

        public void SubscribeToStateChangedEvent(bool enable, Action<string, SaveState> onStateChanged)
        {
            saveStateChanged -= onStateChanged;
            if (enable) { saveStateChanged += onStateChanged; }
        }
        
        protected void RaiseSaveStateChanged()
        {
            UpdateSyncState(false);
            saveStateChanged?.Invoke(saveable.GetType().ToString(), saveState);
        }

        private void UpdateSyncState(bool setIsSaveStateSynced)
        {
            isSaveStateSynced = setIsSaveStateSynced;
            
            if (syncStateLabel == null) { return; }
            syncStateLabel.text = isSaveStateSynced ? _statusSyncMessage : _statusDesyncMessage;
            syncStateLabel.style.color = isSaveStateSynced ? _statusSyncColor : _statusDesyncColor;
        }
    }
}
