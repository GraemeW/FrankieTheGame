using UnityEngine;

namespace Frankie.ZoneManagement
{
    [RequireComponent(typeof(ZoneHandler))]
    public class Door : MonoBehaviour
    {
        // Tunables
        [SerializeField] GameObject doorContainer = null;

        public void ToggleDoor(bool enable)
        {
            if (doorContainer == null) { return; }

            doorContainer.SetActive(enable);
        }
    }
}
