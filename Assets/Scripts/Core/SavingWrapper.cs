using System.Collections;
using UnityEngine;
using Frankie.Saving;
using Frankie.ZoneManagement;
using System.Collections.Generic;
using Frankie.Stats;

namespace Frankie.Core
{
    [RequireComponent(typeof(SavingSystem))]
    public class SavingWrapper : MonoBehaviour
    {
        // Tunables
        [SerializeField] bool deleteSaveFileOnStart = false;

        // Constants
        const string defaultSaveFile = "save";
        const string sessionFile = "session";
        const string debugFile = "debug";
        const string PLAYER_PREFS_CURRENT_SAVE = "currentSave";

        #region StaticMethods
        public static string GetSaveNameForIndex(int index)
        {
            return string.Concat(defaultSaveFile, "_", index.ToString());
        }

        public static bool GetInfoFromName(string saveName, out string characterName, out int level)
        {
            characterName = "Frankie";
            level = 0;

            string saveNameCharacterNameKey = GetPrefsKey(PrefsKeyType.CharacterName, saveName);
            string saveLevelKey = GetPrefsKey(PrefsKeyType.Level, saveName);

            if (!PlayerPrefs.HasKey(saveNameCharacterNameKey) || !PlayerPrefs.HasKey(saveLevelKey)) { return false; }

            characterName = PlayerPrefs.GetString(saveNameCharacterNameKey);
            level = PlayerPrefs.GetInt(saveLevelKey);
            return true;
        }

        private static string GetPrefsKey(PrefsKeyType prefsKeyType, string saveName)
        {
            if (prefsKeyType == PrefsKeyType.CharacterName)
            {
                return string.Concat(saveName, "_CharacterName");
            }
            else if (prefsKeyType == PrefsKeyType.Level)
            {
                return string.Concat(saveName, "_Level");
            }
            return "";
        }

        private static void SetSavePrefs(string saveName, string characterName, int level)
        {
            PlayerPrefs.SetString(GetPrefsKey(PrefsKeyType.CharacterName, saveName), characterName);
            PlayerPrefs.SetInt(GetPrefsKey(PrefsKeyType.Level, saveName), level);
        }

        private static void DeletePlayerForSceneLoad()
        {
            GameObject playerGameObject = GameObject.FindGameObjectWithTag("Player");
            if (playerGameObject != null) { Destroy(playerGameObject); } // Player reconstructed after scene load (prevents control lock-up)
        }

        public static void LoadStartScene()
        {
            DeletePlayerForSceneLoad();

            SceneLoader sceneLoader = GameObject.FindGameObjectWithTag("SceneLoader")?.GetComponent<SceneLoader>();
            if (sceneLoader == null) { return; }

            sceneLoader.QueueStartScreen();
            Fader fader = FindAnyObjectByType<Fader>();
            fader?.UpdateFadeStateImmediate();
        }

        public static void LoadGameOverScene()
        {
            DeletePlayerForSceneLoad();

            SceneLoader sceneLoader = GameObject.FindGameObjectWithTag("SceneLoader")?.GetComponent<SceneLoader>();
            if (sceneLoader == null) { return; }

            Fader fader = FindAnyObjectByType<Fader>();
            fader?.UpdateFadeState(TransitionType.Zone, sceneLoader.GetGameOverZone());
        }

        public static void LoadGameWinScreen()
        {
            DeletePlayerForSceneLoad();

            SceneLoader sceneLoader = GameObject.FindGameObjectWithTag("SceneLoader")?.GetComponent<SceneLoader>();
            if (sceneLoader == null) { return; }

            Fader fader = FindAnyObjectByType<Fader>();
            fader?.UpdateFadeState(TransitionType.Zone, sceneLoader.GetGameWinZone());
        }
        #endregion

        #region PublicMethods
        public IEnumerable<string> ListSaves()
        {
            return GetComponent<SavingSystem>().ListSaves();
        }

        public bool HasSave(string matchSave)
        {
            foreach (string saveName in ListSaves())
            {
                if (string.Equals(matchSave, saveName))
                {
                    return true;
                }
            }
            return false;
        }

        public void NewGame(string saveName)
        {
            DeletePlayerForSceneLoad();
            Delete(sessionFile); // Clear session before load - avoid conflict w/ save system

            SetCurrentSave(saveName);
            SceneLoader sceneLoader = GameObject.FindGameObjectWithTag("SceneLoader")?.GetComponent<SceneLoader>();
            if (sceneLoader == null) { return; }

            sceneLoader.QueueNewGame();
        }

        public void LoadGame(string saveName)
        {
            Delete(sessionFile); // Clear session before load - avoid conflict w/ save system
            SetCurrentSave(saveName);
            Continue();
        }

        public void LoadSession()
        {
            GetComponent<SavingSystem>().LoadWithinScene(sessionFile);
        }

        public void Continue()
        {
            string saveName = GetCurrentSave();
            if (saveName == null) { return; }

            string currentSave = GetCurrentSave();
            SetCurrentSave(currentSave);
            StartCoroutine(LoadFromSave(currentSave));
            GetComponent<SavingSystem>().CopySaveToSession(saveName, sessionFile);
        }

        public void SaveSession()
        {
            GetComponent<SavingSystem>().Save(sessionFile);
        }

        public void Save()
        {
            string saveName = GetCurrentSave();

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                Player player = playerObject.GetComponent<Player>();
                Party party = player.GetComponent<Party>();
                string characterName = party.GetPartyLeaderName();
                int level = party.GetPartyLeader().GetLevel();

                SetSavePrefs(saveName, characterName, level);
            }

            GetComponent<SavingSystem>().CopySessionToSave(sessionFile, saveName);
        }

        public void Delete()
        {
            string currentSave = GetCurrentSave();
            GetComponent<SavingSystem>().Delete(currentSave);
        }

        public void Delete(string saveName)
        {
            GetComponent<SavingSystem>().Delete(saveName);
        }

        public void DeleteSession()
        {
            GetComponent<SavingSystem>().Delete(sessionFile);
        }

        public void SetSaveToDebug()
        {
            SetCurrentSave(debugFile);
        }

        public void DeleteDebugSave()
        {
            GetComponent<SavingSystem>().Delete(debugFile);
        }
        #endregion

        #region PrivateMethods
        IEnumerator LoadFromSave(string saveFile)
        {
            if (deleteSaveFileOnStart)
            {
                Delete();
                yield break;
            }
            DeletePlayerForSceneLoad();

            yield return GetComponent<SavingSystem>().LoadLastScene(saveFile);

            SceneLoader sceneLoader = GameObject.FindGameObjectWithTag("SceneLoader")?.GetComponent<SceneLoader>();
            if (sceneLoader == null) { yield break; }
            sceneLoader.SetCurrentZoneToCurrentScene();

            Fader fader = FindAnyObjectByType<Fader>();
            fader?.UpdateFadeStateImmediate();
        }

        private void SetCurrentSave(string saveFile)
        {
            PlayerPrefs.SetString(PLAYER_PREFS_CURRENT_SAVE, saveFile);
        }

        private string GetCurrentSave()
        {
            if (!PlayerPrefs.HasKey(PLAYER_PREFS_CURRENT_SAVE)) { return null; }

            return PlayerPrefs.GetString(PLAYER_PREFS_CURRENT_SAVE);
        }
        #endregion
    }

}
