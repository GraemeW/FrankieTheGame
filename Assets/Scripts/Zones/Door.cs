using UnityEngine;

namespace Frankie.Zones
{
    [RequireComponent(typeof(ZoneHandler))]
    public class Door : MonoBehaviour
    {
        // Tunables
        [SerializeField] private GameObject doorContainer;

        public void ToggleDoor(bool enable)
        {
            if (doorContainer == null) { return; }
            doorContainer.SetActive(enable);
        }
    }
}
