using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    public class DisableGameObjectsOnStart : MonoBehaviour
    {
        // Tunables
        [SerializeField] GameObject[] gameObjectsToDisable = null;

        private void Start()
        {
            if (gameObjectsToDisable == null || gameObjectsToDisable.Length == 0) { return; }

            foreach (GameObject gameObject in gameObjectsToDisable)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
