using UnityEngine;
using Frankie.Stats;
using Frankie.Control;

namespace Frankie.Core
{
    [RequireComponent(typeof(PlayerStateMachine))]
    [RequireComponent(typeof(PartyCombatConduit))]

    public class Player : MonoBehaviour
    {
        // Cached References
        PlayerStateMachine playerStateHandler = null;
        PartyCombatConduit partyCombatConduit = null;

        #region StaticFind
        private static string playerTag = "Player";
        public static GameObject FindPlayerObject() => GameObject.FindGameObjectWithTag(playerTag);
        public static Player FindPlayer() => FindPlayerObject()?.GetComponent<Player>();
        public static PlayerStateMachine FindPlayerStateMachine() => FindPlayerObject()?.GetComponent<PlayerStateMachine>();
        #endregion

        #region UnityMethods
        private void Awake()
        {
            VerifySingleton();
            playerStateHandler = GetComponent<PlayerStateMachine>();
            partyCombatConduit = GetComponent<PartyCombatConduit>();
        }

        private void OnEnable()
        {
            playerStateHandler.playerStateChanged += HandlePlayerStateChanged;
        }

        private void OnDisable()
        {
            playerStateHandler.playerStateChanged -= HandlePlayerStateChanged;
        }
        #endregion

        #region PrivateMethods
        private void VerifySingleton()
        {
            // Singleton through standard approach -- do not use persistent object spawner for player
            int numberOfPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None).Length;
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

        private void HandlePlayerStateChanged(PlayerStateType playerState)
        {
            // Any player scene change when party is completely wiped out -> shift to game over
            // Will naturally call on combat end during transition
            if (!partyCombatConduit.IsAnyMemberAlive())
            {
                SavingWrapper.LoadGameOverScene();
            }
        }
        #endregion
    }
}