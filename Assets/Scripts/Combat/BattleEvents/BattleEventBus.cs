using System.Collections.Generic;

namespace Frankie.Combat
{
    public static class BattleEventBus<T> where T : IBattleEvent
    {
        private static List<Event> activeEvents = new List<Event>();

        public delegate void Event(T args);
        public static event Event onEvent;
        public static void Raise(T battleEvent) => onEvent?.Invoke(battleEvent);

        public static void SubscribeToEvent(Event handler)
        {
            activeEvents.Add(handler);
            onEvent += handler;
        }
        public static void UnsubscribeFromEvent(Event handler)
        {
            activeEvents.Remove(handler);
            onEvent -= handler;
        }

        public static void ClearAllSubscriptions()
        {
            foreach (Event handler in activeEvents)
            {
                onEvent -= handler;
            }
            activeEvents.Clear();
        }

        public static void PrintAllEvents()
        {
            foreach (Event handler in activeEvents)
            {
                UnityEngine.Debug.Log(handler.Method.Name);
            }
        }
    }

    public static class BattleEventBus
    {
        
    }
}
