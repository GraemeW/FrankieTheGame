using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    public class PersistentObjectSpawner : MonoBehaviour
    {
        [SerializeField] GameObject persistentObjectPrefab = null;
        [SerializeField] GameObject playerPrefab = null;

        // State
        static bool hasSpawned = false;
        static bool playerSpawned = false;

        // Other Static
        static string PLAYER_SPAWNER_TAG = "PlayerSpawner";

        private void Awake()
        {
            SpawnPersistentObjects();
            SpawnPlayer();
        }

        private void SpawnPersistentObjects()
        {
            if (hasSpawned) { return; }

            GameObject persistentObject = Instantiate(persistentObjectPrefab);
            DontDestroyOnLoad(persistentObject);

            hasSpawned = true;
        }

        private void SpawnPlayer()
        {
            if (playerSpawned) { return; }

            GameObject playerSpawnLocation = GameObject.FindGameObjectWithTag(PLAYER_SPAWNER_TAG);
            if (playerSpawnLocation == null) { return; }

            GameObject playerObject = Instantiate(playerPrefab);
            playerObject.transform.position = playerSpawnLocation.transform.position;
            DontDestroyOnLoad(playerObject);

            playerSpawned = true;
        }
    }
}