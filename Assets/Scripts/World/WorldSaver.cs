using UnityEngine;
using Frankie.Core;

namespace Frankie.Control.Specialization
{
    public class WorldSaver : MonoBehaviour
    {
        [SerializeField] private InteractionEvent onSaveEvent;
        
        public void Save(PlayerStateMachine playerStateMachine)
        {
            onSaveEvent?.Invoke(playerStateMachine);
            SavingWrapper.Save();
        }
    }
}
