using System.Collections.Generic;

namespace Frankie.Combat
{
    public static class BattleEventBus<T> where T : IBattleEvent
    {
        private static List<Event> activeSubscriptions = new List<Event>();

        public delegate void Event(T args);
        public static event Event onEvent;
        public static void Raise(T battleEvent)
        {
            UpdateBattleEventBusState(battleEvent);
            onEvent?.Invoke(battleEvent);
        }

        public static void SubscribeToEvent(Event handler)
        {
            if (activeSubscriptions.Contains(handler)) { return; }

            activeSubscriptions.Add(handler);
            onEvent += handler;
        }
        public static void UnsubscribeFromEvent(Event handler)
        {
            activeSubscriptions.Remove(handler);
            onEvent -= handler;
        }

        public static void ClearAllSubscriptions()
        {
            foreach (Event handler in activeSubscriptions)
            {
                onEvent -= handler;
            }
            onEvent = null;

            activeSubscriptions.Clear();
        }

        private static void UpdateBattleEventBusState(T battleEvent)
        {
            switch (battleEvent.battleEventType)
            {
                case BattleEventType.BattleEnter:
                    BattleEventBus.SetInBattle(true);
                    break;
                case BattleEventType.BattleStateChanged:
                    BattleStateChangedEvent battleStateChangedEvent = battleEvent as BattleStateChangedEvent;
                    if (battleStateChangedEvent != null) { BattleEventBus.SetBattleState(battleStateChangedEvent.battleState); }
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

        public static void SetBattleState(BattleState battleState)
        {
            BattleEventBus.battleState = battleState;
        }
        #endregion

        #region SubscriptionManagement
        public static void SetInBattle(bool inBattle)
        {
            BattleEventBus.inBattle = inBattle;
            if (!inBattle)
            {
                SetBattleState(BattleState.Inactive);
                ClearWithinBattleSubscriptions();
            }
        }

        public static void ClearWithinBattleSubscriptions()
        {
            foreach (BattleEventType eventType in System.Enum.GetValues(typeof(BattleEventType)))
            {
                // Entry and exit persist outside of the scope of the battle unless manually unsubscribed
                if (eventType == BattleEventType.BattleEnter || eventType == BattleEventType.BattleExit) { continue; }

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
                case BattleEventType.BattleActionArmed:
                    BattleEventBus<BattleActionArmedEvent>.ClearAllSubscriptions();
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
