using System;
using System.Collections.Generic;
using Unity.Properties;
using LowDefMustard.UIBox;
using Frankie.Inventory;
using Frankie.Stats;

namespace Frankie.Combat.UI
{
    public sealed class CombatParticipantModel : BindableModel, IDisposable
    {
        // Live readout of a combat participant, shared by every screen that shows a character (slides, status, ...)
        // Note:  Values are raw (unformatted, unlocalized); screens and elements decide how to present them
        
        // State
        private string internalCharacterName;
        private float internalHP;
        private float internalMaxHP;
        private float internalAP;
        private float internalMaxAP;
        private int internalLevel;
        private int internalExperienceToLevel;
        private int internalStatsRevision = 0;
        private IReadOnlyList<PersistentStatus> internalStatusEffects = Array.Empty<PersistentStatus>();
        private readonly Dictionary<PersistentStatus, Action> statusEffectTimeouts = new();
        private readonly List<PersistentStatus> trackedStatusEffects = new();
        private bool isDisposed = false;

        // Cached References
        private readonly BaseStats baseStats;
        private readonly Experience experience;
        private readonly Equipment equipment;

        // Events
        public event Action<StateAlteredInfo> stateAltered; // Raised after the model has updated itself

        // Constructor
        public CombatParticipantModel(CombatParticipant combatParticipant)
        {
            this.combatParticipant = combatParticipant;
            combatParticipant.TryGetComponent(out baseStats);
            combatParticipant.TryGetComponent(out experience);
            combatParticipant.TryGetComponent(out equipment);

            internalCharacterName = combatParticipant.GetCombatName();
            RefreshVitals();
            RefreshStats();
            foreach (PersistentStatus persistentStatus in combatParticipant.GetComponents<PersistentStatus>()) { TrackStatusEffect(persistentStatus); }
            PublishStatusEffects();

            combatParticipant.SubscribeToStateUpdates(HandleStateAltered);
            if (baseStats != null) { baseStats.onLevelUp += HandleLevelUp; }
            if (equipment != null) { equipment.equipmentUpdated += HandleEquipmentUpdated; }
        }

        #region BoundProperties
        [CreateProperty] public string characterName
        {
            get => internalCharacterName;
            private set => SetProperty(ref internalCharacterName, value);
        }

        [CreateProperty] public float hp
        {
            get => internalHP;
            private set => SetProperty(ref internalHP, value);
        }

        [CreateProperty] public float maxHP
        {
            get => internalMaxHP;
            private set => SetProperty(ref internalMaxHP, value);
        }

        [CreateProperty] public float ap
        {
            get => internalAP;
            private set => SetProperty(ref internalAP, value);
        }

        [CreateProperty] public float maxAP
        {
            get => internalMaxAP;
            private set => SetProperty(ref internalMaxAP, value);
        }

        [CreateProperty] public int level
        {
            get => internalLevel;
            private set => SetProperty(ref internalLevel, value);
        }

        [CreateProperty] public int experienceToLevel
        {
            get => internalExperienceToLevel;
            private set => SetProperty(ref internalExperienceToLevel, value);
        }

        // Bumped whenever GetStat(...) results may have changed (level up, equipment)
        [CreateProperty] public int statsRevision
        {
            get => internalStatsRevision;
            private set => SetProperty(ref internalStatsRevision, value);
        }

        [CreateProperty] public IReadOnlyList<PersistentStatus> statusEffects
        {
            get => internalStatusEffects;
            private set => SetProperty(ref internalStatusEffects, value);
        }
        #endregion

        #region PublicMethods
        public CombatParticipant combatParticipant { get; }
        public float GetStat(Stat stat) => combatParticipant.GetStat(stat);

        public void Dispose()
        {
            if (isDisposed) { return; }
            isDisposed = true;

            if (combatParticipant != null) { combatParticipant.UnsubscribeToStateUpdates(HandleStateAltered); }
            if (baseStats != null) { baseStats.onLevelUp -= HandleLevelUp; }
            if (equipment != null) { equipment.equipmentUpdated -= HandleEquipmentUpdated; }
            foreach ((PersistentStatus persistentStatus, Action onTimedOut) in statusEffectTimeouts)
            {
                if (persistentStatus != null) { persistentStatus.persistentStatusTimedOut -= onTimedOut; }
            }
            statusEffectTimeouts.Clear();
            trackedStatusEffects.Clear();
            stateAltered = null;
        }
        #endregion

        #region EventHandlers
        private void HandleStateAltered(StateAlteredInfo stateAlteredInfo)
        {
            switch (stateAlteredInfo.stateAlteredType)
            {
                case StateAlteredType.IncreaseHP:
                case StateAlteredType.DecreaseHP:
                case StateAlteredType.AdjustHPNonSpecific:
                case StateAlteredType.AdjustAPNonSpecific: // Note:  AP updates on non-specific adjustments (i.e. even those announced 'quietly')
                case StateAlteredType.Dead:
                case StateAlteredType.Resurrected:
                    RefreshVitals();
                    break;
                case StateAlteredType.StatusEffectApplied:
                    TrackStatusEffect(stateAlteredInfo.persistentStatus);
                    PublishStatusEffects();
                    break;
            }
            stateAltered?.Invoke(stateAlteredInfo);
        }

        private void HandleLevelUp(BaseStats levelledBaseStats, int newLevel, Dictionary<Stat, float> levelUpSheet) => RefreshStats();
        private void HandleEquipmentUpdated(EquipableItemBase equipableItem) => RefreshStats();
        #endregion

        #region PrivateMethods
        private void RefreshVitals()
        {
            hp = combatParticipant.GetHP();
            ap = combatParticipant.GetAP();
        }

        private void RefreshStats()
        {
            maxHP = combatParticipant.GetMaxHP();
            maxAP = combatParticipant.GetMaxAP();
            level = combatParticipant.GetLevel();
            experienceToLevel = experience != null ? experience.GetExperienceRequiredToLevel() : 0;
            RefreshVitals();
            statsRevision++;
        }

        private void TrackStatusEffect(PersistentStatus persistentStatus)
        {
            if (persistentStatus == null || statusEffectTimeouts.ContainsKey(persistentStatus)) { return; }

            statusEffectTimeouts[persistentStatus] = OnTimedOut;
            trackedStatusEffects.Add(persistentStatus);
            persistentStatus.persistentStatusTimedOut += OnTimedOut;
            return;

            // Local Functions
            void OnTimedOut() => UntrackStatusEffect(persistentStatus);
        }

        private void UntrackStatusEffect(PersistentStatus persistentStatus)
        {
            if (!statusEffectTimeouts.Remove(persistentStatus, out Action onTimedOut)) { return; }
            persistentStatus.persistentStatusTimedOut -= onTimedOut;
            trackedStatusEffects.Remove(persistentStatus);
            PublishStatusEffects();
        }

        // Note:  Statuses cancelled without timing out are dropped on every publish
        private void PublishStatusEffects()
        {
            trackedStatusEffects.RemoveAll(persistentStatus => persistentStatus == null);
            statusEffects = trackedStatusEffects.ToArray();
        }
        #endregion
    }
}
