using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public static class BattleEventBus<T> where T : IBattleEvent
    {
        private static List<Event> activeHandlers = new List<Event>();

        public delegate void Event(T args);
        public static event Event onEvent;
        public static void Raise(T battleEvent)
        {
            UpdateBattleEventBusState(battleEvent);
            onEvent?.Invoke(battleEvent);
        }

        public static void SubscribeToEvent(Event handler)
        {
            if (activeHandlers.Contains(handler)) { return; }

            activeHandlers.Add(handler);
            onEvent += handler;
        }
        public static void UnsubscribeFromEvent(Event handler)
        {
            activeHandlers.Remove(handler);
            onEvent -= handler;
        }

        public static void ClearAllSubscriptions()
        {
            foreach (Event handler in activeHandlers)
            {
                onEvent -= handler;
            }
            activeHandlers.Clear();
        }

        private static void UpdateBattleEventBusState(T battleEvent)
        {
            BattleEnterEvent battleStartedEvent = battleEvent as BattleEnterEvent;
            if (battleStartedEvent != null)
            {
                BattleEventBus.SetInBattle(true);
                return;
            }

            BattleStateChangedEvent battleStateChangedEvent = battleEvent as BattleStateChangedEvent;
            if (battleStateChangedEvent != null)
            {
                BattleEventBus.SetBattleState(battleStateChangedEvent.battleState);
                return;
            }

            BattleExitEvent battleExitEvent = battleEvent as BattleExitEvent;
            if (battleExitEvent != null)
            {
                BattleEventBus.SetInBattle(false);
                return;
            }
        }
    }

    public static class BattleEventBus
    {
        #region BattleHandlerState
        public static bool inBattle { get; private set; } = false;
        public static BattleState battleState { get; private set; } = BattleState.Inactive;

        public static void SetInBattle(bool inBattle)
        {
            BattleEventBus.inBattle = inBattle;
            if (!inBattle)
            {
                SetBattleState(BattleState.Inactive);
                DeleteAllSubscriptions();
            }
        }

        public static void SetBattleState(BattleState battleState)
        {
            BattleEventBus.battleState = battleState;
        }
        #endregion

        #region SubscriptionManagement
        public static void DeleteAllSubscriptions()
        {
            foreach (BattleEventType eventType in System.Enum.GetValues(typeof(BattleEventType)))
            {
                DeleteSubscriptions(eventType);
            }
        }

        private static void DeleteSubscriptions(BattleEventType eventType)
        {
            switch (eventType)
            {
                case BattleEventType.BattleStateChanged:
                    BattleEventBus<BattleStateChangedEvent>.ClearAllSubscriptions();
                    break;
            }
        }
        #endregion
    }
}
