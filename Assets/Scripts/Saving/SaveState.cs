using Newtonsoft.Json.Linq;
using System;

namespace Frankie.Saving
{
    [Serializable]
    public class SaveState
    {
        public LoadPriority loadPriority;
        public JToken state;

        public SaveState(LoadPriority loadPriority, object state)
        {
            this.loadPriority = loadPriority;
            this.state = JToken.FromObject(state);
        }

        public object GetState(Type type) => state.ToObject(type); 
        public LoadPriority GetLoadPriority() => loadPriority;
    }
}
