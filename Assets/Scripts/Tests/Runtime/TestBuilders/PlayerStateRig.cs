#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Frankie.Combat;
using Frankie.Core;
using Frankie.Speech;
using Frankie.World;
using Object = UnityEngine.Object;

namespace Frankie.Tests
{
    internal sealed class PlayerStateRig
    {
        // Spawns the Player, WorldUIRoot and Fader prefabs into the test scene
        //  -> Controllers come from the Player's prefab references (dialogue, battle, menus)
        // Editor-only:  prefabs load through AssetDatabase

        // Const Tunables (note that paths are necessarily string ref and brittle, must be updated w/ project layout/re-configuration)
        private const string _playerPrefabPath = "Assets/Game/Core/Player.prefab";
        private const string _worldUIRootPrefabPath = "Assets/Game/UI/Utilities/WorldUIRoot.prefab";
        private const string _faderPrefabPath = "Assets/Game/Core/CoreDep/Fader/Fader.prefab";
        private const float _defaultTimeoutSeconds = 5f;
        private const int _settleFrames = 2;

        // State
        private readonly List<GameObject> spawnedObjects = new();
        public readonly List<PlayerStateType> observedStates = new();
        public PlayerStateMachine playerStateMachine { get; private set; }
        public WorldUIRoot worldUIRoot { get; private set; }

        #region Lifecycle
        public IEnumerator Build()
        {
            worldUIRoot = Spawn(_worldUIRootPrefabPath).GetComponent<WorldUIRoot>();
            Spawn(_faderPrefabPath);
            playerStateMachine = Spawn(_playerPrefabPath).GetComponent<PlayerStateMachine>();
            playerStateMachine.playerStateChanged += RecordState;
            yield return Settle();
        }

        public IEnumerator TearDown()
        {
            if (playerStateMachine != null) { playerStateMachine.playerStateChanged -= RecordState; }
            foreach (GameObject spawnedObject in spawnedObjects) { if (spawnedObject != null) { Object.Destroy(spawnedObject); } }
            spawnedObjects.Clear();
            foreach (DialogueController dialogueController in Object.FindObjectsByType<DialogueController>()) { Object.Destroy(dialogueController.gameObject); }
            foreach (BattleController battleController in Object.FindObjectsByType<BattleController>()) { Object.Destroy(battleController.gameObject); }
            observedStates.Clear();
            yield return null;
        }

        public GameObject Track(GameObject gameObject)
        {
            spawnedObjects.Add(gameObject);
            return gameObject;
        }
        #endregion

        #region Helpers
        public PlayerStateType GetState() => playerStateMachine.GetPlayerStateType();

        public static IEnumerator Settle(int frames = _settleFrames)
        {
            for (int i = 0; i < frames; i++) { yield return null; }
        }

        // Only times out:  follow with the real assertion
        public static IEnumerator WaitUntil(Func<bool> condition, float timeout = _defaultTimeoutSeconds)
        {
            float startTime = Time.realtimeSinceStartup;
            while (!condition() && Time.realtimeSinceStartup - startTime < timeout) { yield return null; }
        }

        private GameObject Spawn(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { throw new InvalidOperationException($"Missing rig prefab at '{prefabPath}'"); }
            return Track(Object.Instantiate(prefab));
        }

        private void RecordState(PlayerStateType playerStateType, IPlayerStateInfo playerStateInfo) => observedStates.Add(playerStateType);
        #endregion
    }
}
#endif
