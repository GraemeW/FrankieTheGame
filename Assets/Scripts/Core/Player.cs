using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;
using Frankie.Control;
using UnityEngine.SceneManagement;
using System;

namespace Frankie.Core
{
    [RequireComponent(typeof(PlayerStateHandler))]
    [RequireComponent(typeof(Party))]

    public class Player : MonoBehaviour
    {
        // Cached References
        PlayerStateHandler playerStateHandler = null;
        Party party = null;

        private void Awake()
        {
            VerifySingleton();
            playerStateHandler = GetComponent<PlayerStateHandler>();
            party = GetComponent<Party>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += UpdateReferencesForNewScene;
            playerStateHandler.playerStateChanged += HandlePlayerStateChanged;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= UpdateReferencesForNewScene;
            playerStateHandler.playerStateChanged -= HandlePlayerStateChanged;
        }

        private void VerifySingleton()
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
        }

        private void UpdateReferencesForNewScene(Scene scene, LoadSceneMode loadSceneMode)
        {
            playerStateHandler.SetWorldCanvas();
        }

        private void HandlePlayerStateChanged(PlayerState playerState)
        {
            // Any player scene change when party is completely wiped out -> shift to game over
            // Will naturally call on combat end during transition
            if (party == null) { return; }

            if (!party.IsAnyMemberAlive())
            {
                SavingWrapper.LoadGameOverScene();
            }
        }
    }
}