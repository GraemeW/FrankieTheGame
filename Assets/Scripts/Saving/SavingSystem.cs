using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Frankie.Saving
{
    public static class SavingSystem
    {
        // Constants
        private const string _saveFileExtension = ".sav";
        private const string _saveLastSceneBuildIndex = "lastSceneBuildIndex";
        private const bool _encryptionEnabled = true;

        // Data Structures
        [System.Serializable]
        private class SaveSuper
        {
            public bool encryptionEnabled;
            public string payload;

            public SaveSuper(bool encryptionEnabled, string payload)
            {
                this.encryptionEnabled = encryptionEnabled;
                this.payload = payload;
            }
        }

        private static string GetPathFromSaveFile(string saveFile) => Path.Combine(Application.persistentDataPath, saveFile + ".sav");
        
        public static IEnumerable<string> ListSaves()
        {
            List<string> saveFiles = Directory.EnumerateFiles(Application.persistentDataPath).ToList();
            foreach (var path in saveFiles.Where(path => Path.GetExtension(path) == _saveFileExtension))
            {
                yield return Path.GetFileNameWithoutExtension(path);
            }
        }
        
        private static List<SaveableEntity> GetAllSaveableEntities()
        {
            List<SaveableEntity> saveableEntities = Object.FindObjectsByType<SaveableEntity>(FindObjectsSortMode.None).ToList();
            foreach (SaveableRoot saveableRoot in Object.FindObjectsByType<SaveableRoot>(FindObjectsSortMode.None)) // Captures inactive game objects
            {
                List<SaveableEntity> rootSaveableEntities = saveableRoot.gameObject.GetComponentsInChildren<SaveableEntity>(true).ToList();
                List<SaveableEntity> combinedSaveableEntities = saveableEntities.Union(rootSaveableEntities).ToList();
                saveableEntities = combinedSaveableEntities;
            }

            return saveableEntities;
        }
        
        public static IEnumerator LoadLastScene(string saveFile)
        {
            JObject state = LoadFile(saveFile);
            string sceneName = SceneManager.GetActiveScene().name;
            if (state.ContainsKey(_saveLastSceneBuildIndex))
            {
                string trySceneName = state[_saveLastSceneBuildIndex]?.ToObject<string>();
                if (!string.IsNullOrWhiteSpace(trySceneName)) { sceneName = trySceneName; }
            }

            yield return SceneManager.LoadSceneAsync(sceneName); 
                // Note:  will throw scene existence error if saved scene name does not exist
                // i.e. scene name changes -> will corrupt save on those scenes -- so don't do it, or prepare for consequences
            RestoreState(state);
        }

        public static void LoadWithinScene(string saveFile)
        {
            JObject state = LoadFile(saveFile);
            RestoreState(state);
        }

        public static void Save(string saveFile)
        {
            JObject state = LoadFile(saveFile);
            CaptureState(state);
            SaveFile(saveFile, state);
        }

        public static void CopySessionToSave(string sessionFile, string saveFile)
        {
            JObject state = LoadFile(sessionFile);
            CaptureState(state);
            SaveFile(saveFile, state);
        }

        public static void CopySaveToSession(string saveFile, string sessionFile)
        {
            JObject state = LoadFile(saveFile);
            CaptureState(state);
            SaveFile(sessionFile, state);
        }

        public static void CopySaveToSave(string inputSaveFile, string copySaveFile)
        {
            JObject state = LoadFile(inputSaveFile);
            SaveFile(copySaveFile, state);
        }

        public static void Append(string sessionFile, SaveableEntity saveableEntity)
        {
            JObject state = LoadFile(sessionFile);
            CaptureIndividualState(state, saveableEntity);
            SaveFile(sessionFile, state);
        }

        public static void CopyCorePlayerStateToSave(string saveFile)
        {
            JObject state = LoadFile(saveFile);
            CaptureState(state, true);
            SaveFile(saveFile, state);
        }

        public static void Delete(string saveFile)
        {
            File.Delete(GetPathFromSaveFile(saveFile));
        }

        private static JObject LoadFile(string saveFile)
        {
            string path = GetPathFromSaveFile(saveFile);
            if (!File.Exists(path))
            {
                return new JObject();
            }

            // Binary Formatter Method -- Deprecated
            /*
            using (FileStream stream = File.Open(path, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (Dictionary<string, object>)formatter.Deserialize(stream);
            }
            */

            // JSON Method
            using (StreamReader textReader = File.OpenText(path))
            {
                using (var reader = new JsonTextReader(textReader))
                {
                    reader.FloatParseHandling = FloatParseHandling.Double;

                    var saveSuper = JObject.Load(reader).ToObject<SaveSuper>();
                    if (saveSuper.encryptionEnabled)
                    {
                        string decryptedPayload = SymmetricEncryptor.DecryptToString(saveSuper.payload);
                        return JToken.Parse(decryptedPayload) as JObject;
                    }
                    else
                    {
                        return JToken.Parse(saveSuper.payload) as JObject;
                    }

                }
            }
        }

        private static void SaveFile(string saveFile, JObject state)
        {
            string path = GetPathFromSaveFile(saveFile);
            Debug.Log($"Saving to {path}");

            // Binary Formatter Method
            /*
            using (FileStream stream = File.Open(path, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, state);
            }
            */

            // JSON Method


            using (StreamWriter textWriter = File.CreateText(path))
            {
                string payload = state.ToString();
                if (_encryptionEnabled)
                {
                    payload = SymmetricEncryptor.EncryptString(state.ToString());
                }
                JToken saveSuper = JToken.FromObject(new SaveSuper(_encryptionEnabled, payload));
                using (JsonTextWriter writer = new JsonTextWriter(textWriter))
                {
                    writer.Formatting = Formatting.Indented;
                    saveSuper.WriteTo(writer);
                }
            }
        }

        private static void CaptureState(JObject state, bool onlyCorePlayerState = false)
        {
            List<SaveableEntity> saveableEntities = GetAllSaveableEntities();
            foreach (SaveableEntity saveable in saveableEntities)
            {
                if (!state.TryGetValue(saveable.GetUniqueIdentifier(), out JToken existingTokenState)) { existingTokenState = new JObject(); }
                state[saveable.GetUniqueIdentifier()] = saveable.CaptureState(existingTokenState, onlyCorePlayerState);
            }

            if (!onlyCorePlayerState) { state[_saveLastSceneBuildIndex] = SceneManager.GetActiveScene().name; }
        }

        private static void CaptureIndividualState(JObject state, SaveableEntity saveable)
        {
            if (saveable == null) { return; }
            
            if (!state.TryGetValue(saveable.GetUniqueIdentifier(), out JToken existingTokenState)) { existingTokenState = new JObject(); }
            state[saveable.GetUniqueIdentifier()] = saveable.CaptureState(existingTokenState);
        }

        private static void RestoreState(JObject state)
        {
            // First Pass -- Object instantiation
            List<SaveableEntity> saveableEntities = GetAllSaveableEntities();
            foreach (SaveableEntity saveable in saveableEntities)
            {
                string id = saveable.GetUniqueIdentifier();
                if (state.TryGetValue(id, out JToken value))
                {
                    saveable.RestoreState(value, LoadPriority.ObjectInstantiation);
                }
            }

            // Second Pass -- Property loading
            saveableEntities = GetAllSaveableEntities();
            foreach (SaveableEntity saveable in saveableEntities)
            {
                string id = saveable.GetUniqueIdentifier();
                if (state.TryGetValue(id, out JToken value))
                {
                    saveable.RestoreState(value, LoadPriority.ObjectProperty);
                }
            }
        }
    }
}
