using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Saving;
using LowDefMustard.Zones;
using Frankie.Core;
using Frankie.Zones;
using Frankie.Stats;

namespace Frankie.Saving
{
    public static partial class SaveFileManager
    {
        // Constants
        private const string _defaultSaveFile = "save";
        private const string _sessionFile = "session";
        
        // Events
        [AutoStaticsCleanup] public static event Action gameListUpdated;

        #region StaticMethods
        public static string GetSaveNameForIndex(int index) => string.Concat(_defaultSaveFile, "_", index.ToString(CultureInfo.InvariantCulture));

        public static bool GetInfoFromSave(string saveName, out string characterName, out int level)
        {
            characterName = "Frankie";
            level = 0;
            if (PlayerPrefsController.CurrentSaveLeaderKeyExists(saveName)) { characterName = PlayerPrefsController.GetCurrentSaveLeader(saveName); }
            if (PlayerPrefsController.CurrentSaveLevelKeyExists(saveName)) { level = PlayerPrefsController.GetCurrentSaveLevel(saveName); }
            return true;
        }

        public static void LoadStartScene()
        {
            SceneLoader.QueueScene(SceneQueueType.Start, new SceneQueueData());
            Fader.StartQuickSceneLoadFade();
        }
        #endregion

        #region PublicMethods
        public static bool HasSave(string matchSave) => ListSaves().Any(saveName => string.Equals(matchSave, saveName));
        public static string GetCurrentSaveName() => PlayerPrefsController.CurrentSaveKeyExists() ? PlayerPrefsController.GetCurrentSave() : null;
        public static void SetCurrentSave(string saveName, bool announceGameListUpdate = true)
        {
            if (string.IsNullOrEmpty(saveName)) { return; }
            PlayerPrefsController.SetCurrentSave(saveName);
            PlayerPrefsController.AnnounceFrameColour(saveName);
            if (announceGameListUpdate) { gameListUpdated?.Invoke(); }
        }
        
        public static IEnumerable<string> ListSaves(bool includeSession = true)
        {
            return includeSession ? SavingSystem.ListSaves() : SavingSystem.ListSaves().Where(saveName => saveName != _sessionFile).ToList();
        }

        public static void NewGame(string saveName)
        {
            DeleteSession();

            SetCurrentSave(saveName);
            var sceneQueueData = new SceneQueueData(null, 0f, false);
            SceneLoader.QueueScene(SceneQueueType.Naming, sceneQueueData);
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
            if (string.IsNullOrEmpty(saveName)) { return; }
            
            DeleteSession();
            
            // Need a MonoBehaviour to kick off a Coroutine, SceneLoader is safe to use 
            SceneLoaderBase sceneLoader = SceneLoaderBase.FindSceneLoader();
            if (sceneLoader == null) { return; }
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

        public static void RestorePropertiesFromSession(SaveableEntity saveableEntity)
        {
            if (saveableEntity == null) { return; }
            saveableEntity.RestoreState(SavingSystem.ManualGetStateEntityToken(_sessionFile, saveableEntity), LoadPriority.ObjectProperty);
        }

        public static void SaveCorePlayerStateToSave()
        {
            string saveName = GetCurrentSaveName();
            if (string.IsNullOrEmpty(saveName)) { return; }
            
            UpdateSavePrefs(saveName);
            SavingSystem.CopyCorePlayerStateToSave(saveName);
        }

        public static void Save(bool announceGameListUpdate = true)
        {
            string saveName = GetCurrentSaveName();
            if (string.IsNullOrEmpty(saveName)) { return; }
            
            UpdateSavePrefs(saveName);
            SavingSystem.CopySessionToSave(_sessionFile, saveName);
            if (announceGameListUpdate) { gameListUpdated?.Invoke(); }
        }

        public static void Delete(bool announceGameListUpdate = true)
        {
            string saveName = GetCurrentSaveName();
            if (string.IsNullOrEmpty(saveName)) { return; }
            
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
            if (string.IsNullOrEmpty(saveName)) { return; }
            
            CopySave(saveName, newSave, announceGameListUpdate);
        }

        public static void CopySave(string existingSaveName, string newSaveName, bool announceGameListUpdate = true)
        {
            if (string.IsNullOrEmpty(existingSaveName)  || string.IsNullOrWhiteSpace(newSaveName)) { return; }
            if (!HasSave(existingSaveName)) { return; }
            
            SavingSystem.CopySaveToSave(existingSaveName, newSaveName);

            if (GetInfoFromSave(existingSaveName, out string characterName, out int level))
            {
                PlayerPrefsController.SetCurrentSaveLeader(characterName, newSaveName);
                PlayerPrefsController.SetCurrentSaveLevel(level, newSaveName);
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
            int level = party.TryGetPartyLeader(out BaseStats partyLeader) ? partyLeader.GetLevel() : 1;

            PlayerPrefsController.SetCurrentSaveLeader(characterName, saveName);
            PlayerPrefsController.SetCurrentSaveLevel(level, saveName);
        }
        
        private static IEnumerator LoadFromSave(string saveFile)
        {
            yield return SavingSystem.LoadLastScene(saveFile);
            SceneLoaderBase.SetCurrentZoneToCurrentScene();
            Fader.StartQuickSceneLoadFade();
        }
        #endregion
    }
}
