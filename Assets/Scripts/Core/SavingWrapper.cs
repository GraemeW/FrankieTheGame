using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private const string _playerPrefsCurrentSave = "currentSave";
        
        // Events
        public static event Action gameListUpdated;

        #region StaticMethods
        public static string GetSaveNameForIndex(int index) => string.Concat(_defaultSaveFile, "_", index.ToString());

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
            // Standard Behaviour:  Load to GameOver scene while skipping session saving
            // From GameOver scene only player will be present, and we can save session to carry over player exp, etc.
            
            SceneLoader sceneLoader = SceneLoader.FindSceneLoader();
            if (sceneLoader == null) { return; }

            Fader fader = Fader.FindFader();
            fader?.UpdateFadeState(TransitionType.Zone, sceneLoader.GetGameOverZone(), false);
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
        public static bool HasSave(string matchSave) => ListSaves().Any(saveName => string.Equals(matchSave, saveName));
        public static string GetCurrentSaveName() => PlayerPrefs.HasKey(_playerPrefsCurrentSave) ? PlayerPrefs.GetString(_playerPrefsCurrentSave) : null;
        public static void SetCurrentSave(string saveFile, bool announceGameListUpdate = true)
        {
            PlayerPrefs.SetString(_playerPrefsCurrentSave, saveFile);
            if (announceGameListUpdate) { gameListUpdated?.Invoke(); }
        }
        
        public static IEnumerable<string> ListSaves(bool includeSession = true)
        {
            return includeSession ? SavingSystem.ListSaves() : SavingSystem.ListSaves().Where(saveName => saveName != _sessionFile).ToList();
        }

        public static void NewGame(string saveName, Zone newGameZoneOverride = null)
        {
            Delete(_sessionFile); // Clear session before load - avoid conflict w/ save system

            SetCurrentSave(saveName);
            SceneLoader sceneLoader = SceneLoader.FindSceneLoader();
            if (sceneLoader == null) { return; }

            sceneLoader.QueueNewGame(() => Save(), newGameZoneOverride);
        }

        public static void LoadGame(string saveName)
        {
            SetCurrentSave(saveName);
            Continue();
        }

        public static void LoadSession()
        {
            SavingSystem.LoadWithinScene(_sessionFile);
        }

        public static void Continue()
        {
            string saveName = GetCurrentSaveName();
            if (saveName == null) { return; }
            
            Delete(_sessionFile); // Clear session before load - avoid conflict w/ save system
            
            SceneLoader sceneLoader = SceneLoader.FindSceneLoader();
            sceneLoader.StartCoroutine(LoadFromSave(saveName));
            SavingSystem.CopySaveToSession(saveName, _sessionFile);
        }

        public static void SaveSession()
        {
            SavingSystem.Save(_sessionFile);
        }

        public static void AppendToSession(SaveableEntity saveableEntity)
        {
            SavingSystem.Append(_sessionFile, saveableEntity);
        }

        public static void SaveCorePlayerStateToSave()
        {
            string saveName = GetCurrentSaveName();
            if (saveName == null) { return; }
            
            UpdateSavePrefs(saveName);
            SavingSystem.CopyCorePlayerStateToSave(saveName);
        }

        public static void Save(bool announceGameListUpdate = true)
        {
            string saveName = GetCurrentSaveName();
            if (saveName == null) { return; }
            
            UpdateSavePrefs(saveName);
            SavingSystem.CopySessionToSave(_sessionFile, saveName);
            if (announceGameListUpdate) { gameListUpdated?.Invoke(); }
        }

        public static void Delete(bool announceGameListUpdate = true)
        {
            string saveName = GetCurrentSaveName();
            if (saveName == null) { return; }
            
            SavingSystem.Delete(saveName);
            if (announceGameListUpdate) { gameListUpdated?.Invoke(); }
        }

        public static void Delete(string saveName, bool announceGameListUpdate = true)
        {
            SavingSystem.Delete(saveName);
            if (announceGameListUpdate) { gameListUpdated?.Invoke(); }
        }

        public static void DeleteSession()
        {
            SavingSystem.Delete(_sessionFile);
        }

        public static void CopySave(string newSave, bool announceGameListUpdate = true)
        {
            string saveName = GetCurrentSaveName();
            if (saveName == null) { return; }
            
            CopySave(saveName, newSave, announceGameListUpdate);
        }

        public static void CopySave(string existingSave, string newSave, bool announceGameListUpdate = true)
        {
            if (string.IsNullOrWhiteSpace(existingSave) || string.IsNullOrWhiteSpace(newSave)) { return; }
            if (!HasSave(existingSave)) { return; }
            
            SavingSystem.CopySaveToSave(existingSave, newSave);

            if (GetInfoFromName(existingSave, out string characterName, out int level))
            {
                SetSavePrefs(newSave, characterName, level);
            }
            if (announceGameListUpdate) { gameListUpdated?.Invoke(); }
        }
        #endregion

        #region PrivateMethods

        private static void UpdateSavePrefs(string saveName)
        {
            Player player = Player.FindPlayer();
            if (player == null) return;
            
            var party = player.GetComponent<Party>();
            string characterName = party.GetPartyLeaderName();
            int level = party.GetPartyLeader().GetLevel();

            SetSavePrefs(saveName, characterName, level);
        }
        
        private static IEnumerator LoadFromSave(string saveFile)
        {
            yield return SavingSystem.LoadLastScene(saveFile);

            SceneLoader sceneLoader = SceneLoader.FindSceneLoader();
            if (sceneLoader == null) { yield break; }
            sceneLoader.SetCurrentZoneToCurrentScene();

            Fader fader = Fader.FindFader();
            fader?.UpdateFadeStateImmediate();
        }
        #endregion
    }
}
