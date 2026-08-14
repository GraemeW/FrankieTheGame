using Newtonsoft.Json.Linq;
using System;

namespace LowDefMustard.Saving
{
    [Serializable]
    public class SaveState
    {
        public LoadPriority loadPriority;
#pragma warning disable UAC1001
        // Unity serialization error, but serialization is OK by Newtonsoft
        public JToken state;
#pragma warning restore UAC1001

        public SaveState(LoadPriority loadPriority, object state)
        {
            this.loadPriority = loadPriority;
            this.state = JToken.FromObject(state);
        }

        public bool TryGetState<T>(out T value) => state.TryToObject(out value); 
        public LoadPriority GetLoadPriority() => loadPriority;
    }
}
