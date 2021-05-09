using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;
using Frankie.Control;
using UnityEngine.SceneManagement;

namespace Frankie.Core
{
    [RequireComponent(typeof(PlayerStateHandler))]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerMover))]
    [RequireComponent(typeof(Party))]

    public class Player : MonoBehaviour
    {
        // Cached References
        PlayerStateHandler playerStateHandler = null;

        private void Awake()
        {
            int numberOfPlayers = FindObjectsOfType<Player>().Length;
            if (numberOfPlayers > 1)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
            else
            {
                DontDestroyOnLoad(gameObject);
            }
            playerStateHandler = GetComponent<PlayerStateHandler>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += UpdateReferencesForNewScene;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= UpdateReferencesForNewScene;
        }

        private void UpdateReferencesForNewScene(Scene scene, LoadSceneMode loadSceneMode)
        {
            playerStateHandler.SetWorldCanvas();
        }
    }

}
