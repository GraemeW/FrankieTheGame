using UnityEngine;

namespace Frankie.Core
{
    public class PersistentObjectSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject persistentObjectPrefab;

        // State
        private static bool _hasSpawned = false;

        private void Awake()
        {
            SpawnPersistentObjects();
        }

        private void SpawnPersistentObjects()
        {
            if (_hasSpawned) { return; }
            if (persistentObjectPrefab == null) { return; }

            GameObject persistentObject = Instantiate(persistentObjectPrefab);
            DontDestroyOnLoad(persistentObject);
            _hasSpawned = true;
        }
    }
}
