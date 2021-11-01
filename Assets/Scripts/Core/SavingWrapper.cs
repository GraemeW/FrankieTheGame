using System.Collections;
using UnityEngine;
using Frankie.Saving;
using Frankie.ZoneManagement;
using System.Collections.Generic;
using Frankie.Stats;
using Frankie.Combat;

namespace Frankie.Core
{
    public class SavingWrapper : MonoBehaviour
    {
        // Tunables
        [SerializeField] bool deleteSaveFileOnStart = false;

        // Constants
        const string defaultSaveFile = "save";
        const string defaultSessionFile = "session";
        const string PLAYER_PREFS_CURRENT_SAVE = "currentSave";

        // Cached References
        PlayerInput playerInput = null;

        // Static Methods
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

        IEnumerator LoadFromSave(string saveFile)
        {
            if (deleteSaveFileOnStart)
            {
                Delete();
                yield break;
            }
            GameObject playerGameObject = GameObject.FindGameObjectWithTag("Player"); 
            if (playerGameObject != null) { Destroy(playerGameObject); } // Player reconstructed after scene load (prevents control lock-up)

            yield return GetComponent<SavingSystem>().LoadLastScene(saveFile);

            SceneLoader sceneLoader = GameObject.FindGameObjectWithTag("SceneLoader").GetComponent<SceneLoader>();
            sceneLoader.SetCurrentZoneToCurrentScene();
            Fader fader = FindObjectOfType<Fader>();
            fader.UpdateFadeStateImmediate();
        }

        private void Awake()
        {
            playerInput = new PlayerInput();

            playerInput.Debug.Save.performed += context => Save();
            playerInput.Debug.Load.performed += context => Continue();
            playerInput.Debug.Delete.performed += context => Delete();
        }

        private void OnEnable()
        {
            playerInput.Debug.Enable();
        }

        private void OnDisable()
        {
            playerInput.Debug.Disable();
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
            SetCurrentSave(saveName);
            SceneLoader sceneLoader = GameObject.FindGameObjectWithTag("SceneLoader").GetComponent<SceneLoader>();
            sceneLoader.QueueNewGame();
        }


        public void LoadSession()
        {
            string currentSave = GetCurrentSave();
            GetComponent<SavingSystem>().LoadWithinScene(currentSave);
        }

        public void Load(string saveName)
        {
            SetCurrentSave(saveName);
            Continue();
        }

        public void Continue()
        {
            string saveName = GetCurrentSave();
            if (saveName == null) { return; }

            string currentSave = GetCurrentSave();
            SetCurrentSave(currentSave);
            StartCoroutine(LoadFromSave(currentSave));
        }

        public void SaveSession()
        {
            string currentSave = GetCurrentSave();
            GetComponent<SavingSystem>().Save(currentSave);
        }

        public void Save()
        {
            string saveName = GetCurrentSave();

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                Player player = playerObject.GetComponent<Player>();
                Party party = player.GetComponent<Party>();
                CombatParticipant partyLeader = party.GetPartyLeader();
                string characterName = partyLeader.GetCombatName();
                int level = partyLeader.GetLevel();

                SetSavePrefs(saveName, characterName, level);
            }

            GetComponent<SavingSystem>().CopySessionToSave(defaultSessionFile, saveName);
        }

        public void Delete()
        {
            string currentSave = GetCurrentSave();
            GetComponent<SavingSystem>().Delete(currentSave);
        }
    }

}
