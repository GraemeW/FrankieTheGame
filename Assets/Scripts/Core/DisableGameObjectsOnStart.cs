using UnityEngine;

namespace Frankie.Core
{
    public class DisableGameObjectsOnStart : MonoBehaviour
    {
        // Tunables
        [SerializeField] private GameObject[] gameObjectsToDisable;

        private void Start()
        {
            if (gameObjectsToDisable == null || gameObjectsToDisable.Length == 0) { return; }

            foreach (GameObject item in gameObjectsToDisable)
            {
                item.SetActive(false);
            }
        }
    }
}
