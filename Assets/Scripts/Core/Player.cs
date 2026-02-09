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
        private PlayerStateMachine playerStateMachine;
        private PartyCombatConduit partyCombatConduit;

        #region StaticFind
        private const string _playerTag = "Player";
        private const string _playerMaskName = "Player";
        private const string _immunePlayerTag = "ImmunePlayer";
        public static int GetPlayerLayer() => LayerMask.NameToLayer(_playerMaskName);
        public static int GetImmunePlayerLayer() => LayerMask.NameToLayer(_immunePlayerTag);
        
        public static GameObject FindPlayerObject() => GameObject.FindGameObjectWithTag(_playerTag);
        public static Player FindPlayer()
        {
            var playerGameObject = FindPlayerObject();
            return playerGameObject != null ? playerGameObject.GetComponent<Player>() : null;
        }
        public static PlayerStateMachine FindPlayerStateMachine()
        {
            var playerGameObject = FindPlayerObject();
            return playerGameObject != null ? playerGameObject.GetComponent<PlayerStateMachine>() : null;
        }

        public static PlayerController FindPlayerController()
        {
            var playerGameObject = FindPlayerObject();
            return playerGameObject != null ? playerGameObject.GetComponent<PlayerController>() : null;
        }
        #endregion

        #region UnityMethods
        private void Awake()
        {
            playerStateMachine = GetComponent<PlayerStateMachine>();
            partyCombatConduit = GetComponent<PartyCombatConduit>();
            VerifySingleton();
        }

        private void OnEnable()
        {
            playerStateMachine.playerStateChanged += HandlePlayerStateChanged;
        }

        private void OnDisable()
        {
            playerStateMachine.playerStateChanged -= HandlePlayerStateChanged;
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

        private void HandlePlayerStateChanged(PlayerStateType playerState, IPlayerStateContext playerStateContext)
        {
            // On player state change, load game over -- skip cutscene transition to allow for player locking
                // Early return on cutscene required or endless loop b/w cutscene state change -> enter cutscene   
            // Note:  This will naturally call on combat end during transition
            if (playerState == PlayerStateType.InCutScene) { return; }
            if (partyCombatConduit.IsAnyMemberAlive()) { return; }
            
            playerStateMachine.EnterCutscene(true, false);
            SavingWrapper.LoadGameOverScene();
        }
        #endregion
    }
}
