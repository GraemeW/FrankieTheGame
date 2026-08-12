using System;
using Newtonsoft.Json.Linq;

namespace LowDefMustard.Saving.Editor
{
    public class SceneSelectorContext
    {
        public JObject SaveState { get; }
        public Action OnSceneDataChanged { get; }
        public Action OnReloadRequested { get; }

        public SceneSelectorContext(JObject saveState, Action onSceneDataChanged, Action onReloadRequested)
        {
            SaveState = saveState;
            OnSceneDataChanged = onSceneDataChanged;
            OnReloadRequested = onReloadRequested;
        }
    }
}