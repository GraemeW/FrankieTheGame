using System.Collections;
using UnityEngine;
using Frankie.Saving;
using Frankie.ZoneManagement;
using System.Collections.Generic;
using Frankie.Stats;

namespace Frankie.Core
{
    public static class SavingWrapper
    {
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

        public static void LoadStartScene()
        {
            SceneLoader sceneLoader = SceneLoader.FindSceneLoader();
            if (sceneLoader == null) { return; }

            sceneLoader.QueueStartScreen();
            Fader fader = GameObject.FindAnyObjectByType<Fader>();
            fader?.UpdateFadeStateImmediate();
        }

        public static void LoadGameOverScene()
        {
            SceneLoader sceneLoader = SceneLoader.FindSceneLoader();
            if (sceneLoader == null) { return; }

            Fader fader = GameObject.FindAnyObjectByType<Fader>();
            fader?.UpdateFadeState(TransitionType.Zone, sceneLoader.GetGameOverZone());
        }

        public static void LoadGameWinScreen()
        {
            SceneLoader sceneLoader = SceneLoader.FindSceneLoader();
            if (sceneLoader == null) { return; }

            Fader fader = GameObject.FindAnyObjectByType<Fader>();
            fader?.UpdateFadeState(TransitionType.Zone, sceneLoader.GetGameWinZone());
        }
        #endregion

        #region PublicMethods
        public static IEnumerable<string> ListSaves()
        {
            return SavingSystem.ListSaves();
        }

        public static bool HasSave(string matchSave)
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

        public static void NewGame(string saveName)
        {
            Delete(sessionFile); // Clear session before load - avoid conflict w/ save system

            SetCurrentSave(saveName);
            SceneLoader sceneLoader = SceneLoader.FindSceneLoader();
            if (sceneLoader == null) { return; }

            sceneLoader.QueueNewGame();
        }

        public static void LoadGame(string saveName)
        {
            Delete(sessionFile); // Clear session before load - avoid conflict w/ save system
            SetCurrentSave(saveName);
            Continue();
        }

        public static void LoadSession()
        {
            SavingSystem.LoadWithinScene(sessionFile);
        }

        public static void Continue()
        {
            string saveName = GetCurrentSave();
            if (saveName == null) { return; }

            string currentSave = GetCurrentSave();
            SetCurrentSave(currentSave);
            SceneLoader sceneLoader = SceneLoader.FindSceneLoader();
            sceneLoader.StartCoroutine(LoadFromSave(currentSave));
            SavingSystem.CopySaveToSession(saveName, sessionFile);
        }

        public static void SaveSession()
        {
            SavingSystem.Save(sessionFile);
        }

        public static void Save()
        {
            string saveName = GetCurrentSave();

            Player player = Player.FindPlayer();
            if (player != null)
            {
                Party party = player.GetComponent<Party>();
                string characterName = party.GetPartyLeaderName();
                int level = party.GetPartyLeader().GetLevel();

                SetSavePrefs(saveName, characterName, level);
            }

            SavingSystem.CopySessionToSave(sessionFile, saveName);
        }

        public static void Delete()
        {
            string currentSave = GetCurrentSave();
            SavingSystem.Delete(currentSave);
        }

        public static void Delete(string saveName)
        {
            SavingSystem.Delete(saveName);
        }

        public static void DeleteSession()
        {
            SavingSystem.Delete(sessionFile);
        }

        public static void SetSaveToDebug()
        {
            SetCurrentSave(debugFile);
        }

        public static void DeleteDebugSave()
        {
            SavingSystem.Delete(debugFile);
        }
        #endregion

        #region PrivateMethods
        private static IEnumerator LoadFromSave(string saveFile)
        {
            yield return SavingSystem.LoadLastScene(saveFile);

            SceneLoader sceneLoader = SceneLoader.FindSceneLoader();
            if (sceneLoader == null) { yield break; }
            sceneLoader.SetCurrentZoneToCurrentScene();

            Fader fader = GameObject.FindAnyObjectByType<Fader>();
            fader?.UpdateFadeStateImmediate();
        }

        private static void SetCurrentSave(string saveFile)
        {
            PlayerPrefs.SetString(PLAYER_PREFS_CURRENT_SAVE, saveFile);
        }

        private static string GetCurrentSave()
        {
            if (!PlayerPrefs.HasKey(PLAYER_PREFS_CURRENT_SAVE)) { return null; }

            return PlayerPrefs.GetString(PLAYER_PREFS_CURRENT_SAVE);
        }
        #endregion
    }
}
