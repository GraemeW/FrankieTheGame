using System;
using System.Collections.Generic;

namespace Frankie.Core.PlayerStateMemory
{
    public class ActionMemory
    {
        // Parameters
        private PlayerStateTypeActionPair actionUnderConsideration;
        private readonly Stack<PlayerStateTypeActionPair> queuedActions = new();
        private bool readyToPopQueue = false;
        
        #region DataStructures
        private class PlayerStateTypeActionPair
        {
            public PlayerStateType playerStateType { get; }
            public Action action { get; }
            
            public PlayerStateTypeActionPair(PlayerStateType playerStateType, Action action)
            {
                this.playerStateType = playerStateType;
                this.action = action;
            }
        }
        #endregion
        
        #region PublicMethods
        public void ResetActionUnderConsideration() => actionUnderConsideration = null;
        public void ClearQueuedActions() => queuedActions.Clear();
        
        public void SetReadyToPop(PlayerStateType playerStateType) => readyToPopQueue = playerStateType == PlayerStateType.InWorld;
        public void SetActionUnderConsideration(PlayerStateType playerStateType, Action action) => actionUnderConsideration = new PlayerStateTypeActionPair(playerStateType, action); 
        
        public void QueueActionUnderConsideration()
        {
            if (actionUnderConsideration?.action == null) { return; }
            queuedActions.Push(actionUnderConsideration);
        }
        
        public void TryPopQueue()
        {
            if (!readyToPopQueue) { return; }
            readyToPopQueue = false; // Either popped queue will change state, or queue invalidated -- clear state
            PopQueuedAction();
        }
        
        public void ChainQueuedCombatAction(PlayerStateType playerStateType = PlayerStateType.InBattle)
        {
            while (true)
            {
                // On combat allow chained queues (e.g. multiple combat instantiation while in dialogue)
                if (playerStateType != PlayerStateType.InBattle) { return; }
                if (queuedActions.Count == 0 || queuedActions.Peek().playerStateType != PlayerStateType.InBattle) { return; }
                if (!TryActivateNextQueuedAction(out playerStateType)) { return; }
            }
        }
        #endregion
        
        #region PrivateMethods
        private void PopQueuedAction()
        {
            if (!TryActivateNextQueuedAction(out PlayerStateType playerStateType)) { return; }
            ChainQueuedCombatAction(playerStateType);
        }

        private bool TryActivateNextQueuedAction(out PlayerStateType playerStateType)
        {
            playerStateType = PlayerStateType.InWorld;
            if (queuedActions.Count == 0) { return false; }
            
            PlayerStateTypeActionPair nextQueuedAction = queuedActions.Pop();
            playerStateType = nextQueuedAction.playerStateType;
            nextQueuedAction.action?.Invoke();
            return true;
        }
        #endregion
    }
}
