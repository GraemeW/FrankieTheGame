using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Saving
{
    [System.Serializable]
    public class SaveState
    {
        LoadPriority loadPriority = LoadPriority.ObjectProperty;
        object state = null;

        public SaveState(LoadPriority loadPriority, object state)
        {
            this.loadPriority = loadPriority;
            this.state = state;
        }

        public object GetState()
        {
            return state;
        }

        public LoadPriority GetLoadPriority()
        {
            return loadPriority;
        }
    }
}