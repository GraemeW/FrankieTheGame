using UnityEngine;
using Frankie.Core;
using Frankie.Control;

namespace Frankie.World
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
