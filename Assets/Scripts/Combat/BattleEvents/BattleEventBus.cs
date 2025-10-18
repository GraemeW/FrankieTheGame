using System.Collections.Generic;

namespace Frankie.Combat
{
    public static class BattleEventBus<T> where T : IBattleEvent
    {
        private static readonly List<Event> _activeSubscriptions = new();

        public delegate void Event(T args);
        public static event Event onEvent;
        public static void Raise(T battleEvent)
        {
            UpdateBattleEventBusState(battleEvent);
            onEvent?.Invoke(battleEvent);
        }

        public static void SubscribeToEvent(Event handler)
        {
            if (_activeSubscriptions.Contains(handler)) { return; }

            _activeSubscriptions.Add(handler);
            onEvent += handler;
        }
        public static void UnsubscribeFromEvent(Event handler)
        {
            _activeSubscriptions.Remove(handler);
            onEvent -= handler;
        }

        public static void ClearAllSubscriptions()
        {
            foreach (Event handler in _activeSubscriptions)
            {
                onEvent -= handler;
            }
            onEvent = null;

            _activeSubscriptions.Clear();
        }

        private static void UpdateBattleEventBusState(T battleEvent)
        {
            switch (battleEvent.battleEventType)
            {
                case BattleEventType.BattleEnter:
                    BattleEventBus.SetInBattle(true);
                    break;
                case BattleEventType.BattleStateChanged:
                    if (battleEvent is BattleStateChangedEvent battleStateChangedEvent) { BattleEventBus.SetBattleState(battleStateChangedEvent.battleState); }
                    break;
                case BattleEventType.BattleExit:
                    BattleEventBus.SetInBattle(false);
                    break;
            }
        }
    }

    public static class BattleEventBus
    {
        #region BattleHandlerState
        public static bool inBattle { get; private set; } = false;
        public static BattleState battleState { get; private set; } = BattleState.Inactive;

        public static void SetBattleState(BattleState setBattleState)
        {
            battleState = setBattleState;
        }
        #endregion

        #region SubscriptionManagement
        public static void SetInBattle(bool setInBattle)
        {
            inBattle = setInBattle;
            if (!inBattle)
            {
                SetBattleState(BattleState.Inactive);
                ClearWithinBattleSubscriptions();
            }
        }

        private static void ClearWithinBattleSubscriptions()
        {
            foreach (BattleEventType eventType in System.Enum.GetValues(typeof(BattleEventType)))
            {
                if (eventType is BattleEventType.BattleEnter or BattleEventType.BattleExit) { continue; }

                ClearSubscriptions(eventType);
            }
        }

        private static void ClearSubscriptions(BattleEventType eventType)
        {
            // Note:  POR is to manually unsubscribe, this is safety
            switch (eventType)
            {
                case BattleEventType.BattleEnter:
                    BattleEventBus<BattleEnterEvent>.ClearAllSubscriptions();
                    break;
                case BattleEventType.BattleStateChanged:
                    BattleEventBus<BattleStateChangedEvent>.ClearAllSubscriptions();
                    break;
                case BattleEventType.BattleEntityAdded:
                    BattleEventBus<BattleEntityAddedEvent>.ClearAllSubscriptions();
                    break;
                case BattleEventType.BattleEntitySelected:
                    BattleEventBus<BattleEntitySelectedEvent>.ClearAllSubscriptions();
                    break;
                case BattleEventType.BattleActionSelected:
                    BattleEventBus<BattleActionSelectedEvent>.ClearAllSubscriptions();
                    break;
                case BattleEventType.BattleActionArmed:
                    BattleEventBus<BattleActionArmedEvent>.ClearAllSubscriptions();
                    break;
                case BattleEventType.BattleQueueUpdated:
                    BattleEventBus<BattleQueueUpdatedEvent>.ClearAllSubscriptions();
                    break;
                case BattleEventType.BattleSequencedProcessed:
                    BattleEventBus<BattleSequenceProcessedEvent>.ClearAllSubscriptions();
                    break;
                case BattleEventType.BattleEntityStateAltered:
                    BattleEventBus<StateAlteredInfo>.ClearAllSubscriptions();
                    break;
                case BattleEventType.BattleEntityRemovedFromBoard:
                    BattleEventBus<BattleEntityRemovedFromBoardEvent>.ClearAllSubscriptions();
                    break;
                case BattleEventType.BattleExit:
                    BattleEventBus<BattleExitEvent>.ClearAllSubscriptions();
                    break;
            }
        }
        #endregion
    }
}
