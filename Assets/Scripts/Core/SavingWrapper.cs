using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Saving;
using Frankie.ZoneManagement;
using Frankie.Stats;

namespace Frankie.Core
{
    public static class SavingWrapper
    {
        // Constants
        private const string _defaultSaveFile = "save";
        private const string _sessionFile = "session";
        private const string _debugFile = "debug";
        private const string _playerPrefsCurrentSave = "currentSave";

        #region StaticMethods
        public static string GetSaveNameForIndex(int index)
        {
            return string.Concat(_defaultSaveFile, "_", index.ToString());
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
            return prefsKeyType switch
            {
                PrefsKeyType.CharacterName => string.Concat(saveName, "_CharacterName"),
                PrefsKeyType.Level => string.Concat(saveName, "_Level"),
                _ => ""
            };
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
            Fader fader = Fader.FindFader();
            fader?.UpdateFadeStateImmediate();
        }

        public static void LoadGameOverScene()
        {
            SceneLoader sceneLoader = SceneLoader.FindSceneLoader();
            if (sceneLoader == null) { return; }

            Fader fader = Fader.FindFader();
            fader?.UpdateFadeState(TransitionType.Zone, sceneLoader.GetGameOverZone());
        }

        public static void LoadGameWinScreen()
        {
            SceneLoader sceneLoader = SceneLoader.FindSceneLoader();
            if (sceneLoader == null) { return; }

            Fader fader = Fader.FindFader();
            fader?.UpdateFadeState(TransitionType.Zone, sceneLoader.GetGameWinZone());
        }
        #endregion

        #region PublicMethods

        private static IEnumerable<string> ListSaves()
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

        public static void NewGame(string saveName, Zone newGameZoneOverride = null)
        {
            Delete(_sessionFile); // Clear session before load - avoid conflict w/ save system

            SetCurrentSave(saveName);
            SceneLoader sceneLoader = SceneLoader.FindSceneLoader();
            if (sceneLoader == null) { return; }

            sceneLoader.QueueNewGame(newGameZoneOverride);
        }

        public static void LoadGame(string saveName)
        {
            Delete(_sessionFile); // Clear session before load - avoid conflict w/ save system
            SetCurrentSave(saveName);
            Continue();
        }

        public static void LoadSession()
        {
            SavingSystem.LoadWithinScene(_sessionFile);
        }

        public static void Continue()
        {
            string saveName = GetCurrentSave();
            if (saveName == null) { return; }

            string currentSave = GetCurrentSave();
            SetCurrentSave(currentSave);
            SceneLoader sceneLoader = SceneLoader.FindSceneLoader();
            sceneLoader.StartCoroutine(LoadFromSave(currentSave));
            SavingSystem.CopySaveToSession(saveName, _sessionFile);
        }

        public static void SaveSession()
        {
            SavingSystem.Save(_sessionFile);
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

            SavingSystem.CopySessionToSave(_sessionFile, saveName);
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
            SavingSystem.Delete(_sessionFile);
        }
        #endregion

        #region PrivateMethods
        private static IEnumerator LoadFromSave(string saveFile)
        {
            yield return SavingSystem.LoadLastScene(saveFile);

            SceneLoader sceneLoader = SceneLoader.FindSceneLoader();
            if (sceneLoader == null) { yield break; }
            sceneLoader.SetCurrentZoneToCurrentScene();

            Fader fader = Fader.FindFader();
            fader?.UpdateFadeStateImmediate();
        }

        private static void SetCurrentSave(string saveFile)
        {
            PlayerPrefs.SetString(_playerPrefsCurrentSave, saveFile);
        }

        private static string GetCurrentSave()
        {
            return (!PlayerPrefs.HasKey(_playerPrefsCurrentSave) ? null : PlayerPrefs.GetString(_playerPrefsCurrentSave)) ?? _debugFile;
        }
        #endregion
    }
}
