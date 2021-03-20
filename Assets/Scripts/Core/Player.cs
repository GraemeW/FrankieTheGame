using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Control;
using Frankie.Stats;
using Frankie.SceneManagement;
using UnityEngine.SceneManagement;

namespace Frankie.Core
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerMover))]
    [RequireComponent(typeof(Party))]

    public class Player : MonoBehaviour
    {
        // Cached References
        PlayerController playerController = null;

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
            playerController = GetComponent<PlayerController>();
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
            playerController.SetWorldCanvas();
        }
    }

}
