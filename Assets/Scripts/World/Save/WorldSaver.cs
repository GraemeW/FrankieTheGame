using UnityEngine;
using Frankie.Core;
using Frankie.Control;
using Frankie.Saving;

namespace Frankie.World
{
    public class WorldSaver : MonoBehaviour
    {
        // Tunables
        [SerializeField] private InteractionEvent onSaveEvent;
        
        #region PublicMethods
        public void Save(PlayerStateMachine playerStateMachine)
        {
            onSaveEvent?.Invoke(playerStateMachine);
            SaveFileManager.Save();
        }
        #endregion
    }
}
