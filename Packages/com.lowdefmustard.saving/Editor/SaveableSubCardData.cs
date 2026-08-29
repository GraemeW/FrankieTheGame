using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Saving.Editor
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
        protected const float entityCardSpacerHeight = 10f;
        
        // State
        public SaveState saveState { get; protected set; }
        protected SaveableEntityCardData saveableEntityCardData;
        protected ISaveableBase saveable;
        // Events
        public event Action<string, SaveState> saveStateChanged;
        // ActiveState
        private bool isSaveStateSynced = true;
        // UI State
        private Box contentContainer;
        private Label syncStateLabel;
        
        #region StaticRegistries
        private static readonly List<(Type type, Func<ISaveableBase, bool> match, Func<ISaveableBase, SaveState, SaveableSubCardData> factory)> _subCardFactories = new();
        private static readonly PriorityRegistry<ISaveableBase> _subCardSortRegistry = new();
        
        public static void RegisterSubCard<T>(Func<T, SaveState, SaveableSubCardData> factory) where T : ISaveableBase
        {
            _subCardFactories.Add((typeof(T), saveable => saveable is T, (saveable, state) => factory((T)saveable, state)));
        }
        public static void UnregisterSubCard<T>() where T : ISaveableBase => _subCardFactories.RemoveAll(entry => entry.type == typeof(T));
        public static void RegisterSubCardPriority(Func<ISaveableBase, bool> match, int priority) => _subCardSortRegistry.Register(match, priority);
        public static void UnregisterSubCardPriority(Func<ISaveableBase, bool> match) => _subCardSortRegistry.Unregister(match);
        
        public static SaveableSubCardData CreateTypeSpecificSubCard(ISaveableBase saveable, SaveState saveState)
        {
            foreach ((Type _, var match, var factory) in _subCardFactories)
            {
                if (match(saveable)) { return factory(saveable, saveState); }
            }
            return new GenericSaveableSubCard(saveable, saveState);
        }
        public static int GetEntitySortPriority(ISaveableBase saveable) => _subCardSortRegistry.GetPriority(saveable);
        #endregion

        #region AbstractMethods
        protected abstract void AddEditableFieldsToSubCardView(Box subCardView);
        #endregion
        
        #region PublicMethods
        public bool IsSaveStateSynced() => isSaveStateSynced;
        public virtual bool IsPlayerMoverSubCard() => false;
        
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
            saveStateChanged?.Invoke(saveable.GetType().ToString(), saveState); // Type ToString does not require CultureInvariant
        }

        private void UpdateSyncState(bool setIsSaveStateSynced)
        {
            isSaveStateSynced = setIsSaveStateSynced;
            
            saveableEntityCardData?.SetIsDataSynced(isSaveStateSynced);
            if (syncStateLabel != null)
            {
                syncStateLabel.text = isSaveStateSynced ? _statusSyncMessage : _statusDesyncMessage;
                syncStateLabel.style.color = isSaveStateSynced ? _statusSyncColor : _statusDesyncColor;
            }
        }
        #endregion
    }
}
