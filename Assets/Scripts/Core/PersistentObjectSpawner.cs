using UnityEngine;

namespace Frankie.Core
{
    public class PersistentObjectSpawner : MonoBehaviour
    {
        [SerializeField] GameObject persistentObjectPrefab = null;

        // State
        static bool hasSpawned = false;

        private void Awake()
        {
            SpawnPersistentObjects();
        }

        private void SpawnPersistentObjects()
        {
            if (hasSpawned) { return; }

            GameObject persistentObject = Instantiate(persistentObjectPrefab);
            DontDestroyOnLoad(persistentObject);

            hasSpawned = true;
        }
    }
}