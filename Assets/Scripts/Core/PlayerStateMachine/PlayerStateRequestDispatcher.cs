using System;
using System.Collections.Generic;
using LowDefMustard.Core;

namespace Frankie.Core
{
    internal sealed class PlayerStateRequestDispatcher
    {
        // Note:
        //  - Handles requests made during an in-progress state change
        //      -> i.e. the stretch while an Enter* call is on the stack -> til SetPlayerState -> playerStateChanged listeners finish
        //  - Drained requests are handled like any other request
        //      -> state can accept it, ignore it, push it to ActionMemory to properly queue it, etc.
        //  - FIFO-deferred (drained before the outermost request returns)

        // Const
        private const int _maxDeferredCount = 16;

        // State
        private readonly Queue<Action> deferredRequests = new();
        private int depth = 0;
        private int inlineDepth = 0;
        private bool draining = false;

        #region PublicMethods
        public void Run(string requestName, Action request)
        {
            if (request == null) { return; }
            if (depth == inlineDepth)
            {
                Execute(request);
                return;
            }

            deferredRequests.Enqueue(request);
            LDMLog.Info(LDMLogCategory.Core, $"Deferred player state request '{requestName}' until the current transition completes.");
        }

        public void RunInline(Action action)
        {
            // Requests made directly by the action run immediately, anything nested below them still defers
            if (action == null) { return; }

            int previousInlineDepth = inlineDepth;
            inlineDepth = depth;
            try { action.Invoke(); }
            finally { inlineDepth = previousInlineDepth; }
        }

        internal bool IsBusy() => depth > 0 || draining;
        internal int GetDeferredCount() => deferredRequests.Count;
        #endregion

        #region PrivateMethods
        private void Execute(Action request)
        {
            depth++;
            try { request.Invoke(); }
            finally
            {
                depth--;
                if (depth == 0) { Drain(); }
            }
        }

        private void Drain()
        {
            if (draining) { return; }

            draining = true;
            try
            {
                int drainCount = 0;
                while (deferredRequests.Count > 0)
                {
                    if (drainCount >= _maxDeferredCount)
                    {
                        LDMLog.Warning(LDMLogCategory.Core, $"Dropped {deferredRequests.Count} deferred player state requests due to possible request loop.");
                        deferredRequests.Clear();
                        break;
                    }

                    Action deferredRequest = deferredRequests.Dequeue();
                    drainCount++;
                    try { Execute(deferredRequest); }
                    catch (Exception exception) { LDMLog.Exception(exception); }
                }
            }
            finally { draining = false; }
        }
        #endregion
    }
}
