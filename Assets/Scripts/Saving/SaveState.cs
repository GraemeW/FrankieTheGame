using Newtonsoft.Json.Linq;
using System;

namespace Frankie.Saving
{
    [System.Serializable]
    public class SaveState
    {
        public LoadPriority loadPriority = LoadPriority.ObjectProperty;
        public JToken state = null;

        public SaveState(LoadPriority loadPriority, object state)
        {
            this.loadPriority = loadPriority;
            this.state = JToken.FromObject(state);
        }

        public object GetState(Type type)
        {
            return state.ToObject(type);
        }

        public LoadPriority GetLoadPriority()
        {
            return loadPriority;
        }
    }
}
