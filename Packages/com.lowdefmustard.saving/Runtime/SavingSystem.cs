using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using LowDefMustard.Core;
using Object = UnityEngine.Object;

namespace LowDefMustard.Saving
{
    public static class SavingSystem
    {
        // Constants
        private const string _saveFileExtension = ".sav";
        private const string _saveLastScene = "lastScene";
        private const bool _encryptionEnabled = true;

        // Editor Hooks
#if UNITY_EDITOR
        [NoAutoStaticsCleanup] public static Func<string, bool> editorIsSceneLoadable; // Set by the project's editor code, e.g. against its scene load mechanism (Addressables)
        [NoAutoStaticsCleanup] public static Func<string> editorGetActiveSceneKey; // Set by the project's editor code ~ the active scene's key, for the Save Editor
#endif

        #region DataStructures
        public delegate IEnumerator SceneLoadRoutine(string sceneKey, Action<bool> onLoadComplete);
        
        [Serializable]
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
        #endregion

        #region Getters
        private static string GetPathFromSaveFile(string saveFile) => Path.Combine(Application.persistentDataPath, saveFile + ".sav");
        
        public static IEnumerable<string> ListSaves()
        {
            List<string> saveFiles = Directory.EnumerateFiles(Application.persistentDataPath).ToList();
            foreach (var path in saveFiles.Where(path => Path.GetExtension(path) == _saveFileExtension))
            {
                yield return Path.GetFileNameWithoutExtension(path);
            }
        }
        
        public static List<SaveableEntity> GetValidSaveableEntities()
        {
            List<SaveableEntity> saveableEntities = Object.FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include).Where(saveableEntity => !saveableEntity.IsSaveRestricted()).ToList();
            foreach (SaveableEntity saveableEntity in saveableEntities.Where(saveableEntity => !saveableEntity.gameObject.activeSelf))
            {
                // Manually toggle enable/disable to ensure base properties set
                saveableEntity.gameObject.SetActive(true);
                saveableEntity.gameObject.SetActive(false);
            }
            LDMLog.Info(LDMLogCategory.Saving, $"Found {saveableEntities.Count} saveable entities.");
            return saveableEntities;
        }
        
        public static JObject ManualGetFullState(string saveFile) => LoadFile(saveFile);

        public static JToken ManualGetStateEntityToken(string saveFile, SaveableEntity saveableEntity)
        {
            JObject fullState = LoadFile(saveFile);
            string id = saveableEntity.GetUniqueIdentifier();
            return fullState.TryGetValue(id, out JToken value) ? value : null;
        }
        #endregion
        
        #region PublicMethods
        public static IEnumerator LoadLastScene(string saveFile, SceneLoadRoutine loadScene)
        {
            if (loadScene == null) { LDMLog.Error(LDMLogCategory.Saving, $"No scene loader given to load save '{saveFile}'. Ignoring the request."); yield break; }
            
            JObject state = LoadFile(saveFile);
            string sceneKey = ManualGetLastScene(state);
            if (string.IsNullOrWhiteSpace(sceneKey))
            {
                LDMLog.Warning(LDMLogCategory.Saving, $"Save '{saveFile}' has no last scene, so its state wasn't restored. Staying in the current scene.");
                yield break;
            }

            var isLoaded = false;
            yield return loadScene(sceneKey, result => isLoaded = result); // Note:  fails if the saved scene no longer exists (e.g. deleted)
            if (!isLoaded)
            {
                LDMLog.Warning(LDMLogCategory.Saving, $"Save '{saveFile}' couldn't load its scene ('{sceneKey}'), so its state wasn't restored. Staying in the current scene.");
                yield break;
            }

            RestoreState(state);
            LDMLog.Important(LDMLogCategory.Saving, $"Loaded save '{saveFile}' (scene: '{sceneKey}').");
        }

        public static void LoadWithinScene(string saveFile)
        {
            JObject state = LoadFile(saveFile);
            RestoreState(state);
        }

        public static void Save(string saveFile, string sceneKey = null)
        {
            JObject state = LoadFile(saveFile);
            CaptureState(state, sceneKey);
            SaveFile(saveFile, state);
        }

        public static void CopySessionToSave(string sessionFile, string saveFile, string sceneKey) // null sceneKey -> keeps current save scene
        {
            JObject state = LoadFile(sessionFile);
            CaptureState(state, sceneKey);
            SaveFile(saveFile, state);
        }

        public static void CopySaveToSession(string saveFile, string sessionFile, string sceneKey) // null sceneKey -> keeps current save scene
        {
            JObject state = LoadFile(saveFile);
            CaptureState(state, sceneKey);
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
            LDMLog.Info(LDMLogCategory.Saving, $"Appended entity state to '{sessionFile}'.");
            SaveFile(sessionFile, state);
        }

        public static void CopyCorePlayerStateToSave(string saveFile)
        {
            JObject state = LoadFile(saveFile);
            CaptureState(state, null, true);
            SaveFile(saveFile, state);
        }

        public static void Delete(string saveFile)
        {
            File.Delete(GetPathFromSaveFile(saveFile));
            LDMLog.Info(LDMLogCategory.Saving, $"Deleted save file '{saveFile}'.");
        }

        public static string ManualGetLastScene(JObject state)
        {
            if (state == null) { return string.Empty; }
            return state.ContainsKey(_saveLastScene) ? state[_saveLastScene]?.ToObject<string>() : string.Empty;
        }

        public static void ManualUpdateLastScene(JObject state, string sceneKey)
        {
            if (string.IsNullOrEmpty(sceneKey)) { LDMLog.Warning(LDMLogCategory.Saving, "Invalid sceneKey (null or empty).  Skipping manual update."); return; }
#if UNITY_EDITOR
            if (editorIsSceneLoadable != null && !editorIsSceneLoadable(sceneKey)) { LDMLog.Warning(LDMLogCategory.Saving, $"Last scene set to '{sceneKey}', but it can't be loaded by that key at runtime. The save may not load correctly."); }
#endif
            state[_saveLastScene] = sceneKey;
        }

        public static void ManualAddOverWriteToState(JObject state, JToken stateToAdd, string uniqueIdentifier)
        {
            if (string.IsNullOrEmpty(uniqueIdentifier)) { return; }
            stateToAdd ??= new JObject();
            state[uniqueIdentifier] = stateToAdd;
        }

        public static void ManualSave(string saveFile, JObject state)
        {
            SaveFile(saveFile, state);
        }
        #endregion

        #region PrivateMethods
        private static JObject LoadFile(string saveFile)
        {
            string path = GetPathFromSaveFile(saveFile);
            if (!File.Exists(path))
            {
                LDMLog.Info(LDMLogCategory.Saving, $"No save file '{saveFile}' found. Using an empty state.");
                return new JObject();
            }
            
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
            LDMLog.Info(LDMLogCategory.Saving, $"Writing save file '{saveFile}' (path: '{path}').");
            
            using (StreamWriter textWriter = File.CreateText(path))
            {
                // Note - When using standard SerializeObject() methods, default setting for JsonSerializationSettings is:  InvariantCulture
                // See:  https://www.newtonsoft.com/json/help/html/P_Newtonsoft_Json_JsonSerializerSettings_Culture.htm
                
                string payload = JsonConvert.SerializeObject(state, Formatting.None);
                if (_encryptionEnabled)
                {
                    payload = SymmetricEncryptor.EncryptString(payload);
                }
                JToken saveSuper = JToken.FromObject(new SaveSuper(_encryptionEnabled, payload));
                using (JsonTextWriter writer = new JsonTextWriter(textWriter))
                {
                    writer.Formatting = Formatting.Indented;
                    saveSuper.WriteTo(writer);
                }
            }
        }

        private static void CaptureState(JObject state, string sceneKey, bool onlyCorePlayerState = false)
        {
            foreach (SaveableEntity saveable in GetValidSaveableEntities())
            {
                if (!state.TryGetValue(saveable.GetUniqueIdentifier(), out JToken existingTokenState)) { existingTokenState = new JObject(); }
                if (!saveable.TryCaptureState(existingTokenState, out JToken updatedTokenState, onlyCorePlayerState)) { continue; }
                state[saveable.GetUniqueIdentifier()] = updatedTokenState;
            }

            if (!onlyCorePlayerState && !string.IsNullOrWhiteSpace(sceneKey)) { state[_saveLastScene] = sceneKey; } // Blank keeps the existing last scene
        }

        private static void CaptureIndividualState(JObject state, SaveableEntity saveable)
        {
            if (saveable == null) { return; }
            if (saveable.IsSaveRestricted()) { return; }
            
            if (!state.TryGetValue(saveable.GetUniqueIdentifier(), out JToken existingTokenState)) { existingTokenState = new JObject(); }
            if (!saveable.TryCaptureState(existingTokenState, out JToken updatedTokenState)) { return; }
            state[saveable.GetUniqueIdentifier()] = updatedTokenState;
        }

        private static void RestoreState(JObject state)
        {
            // First Pass -- Object instantiation
            List<SaveableEntity> saveableEntities = GetValidSaveableEntities();
            foreach (SaveableEntity saveable in saveableEntities)
            {
                string id = saveable.GetUniqueIdentifier();
                if (state.TryGetValue(id, out JToken value))
                {
                    saveable.RestoreState(value, LoadPriority.ObjectInstantiation);
                }
            }

            // Second Pass -- Property loading
            saveableEntities = GetValidSaveableEntities();
            foreach (SaveableEntity saveable in saveableEntities)
            {
                string id = saveable.GetUniqueIdentifier();
                if (state.TryGetValue(id, out JToken value))
                {
                    saveable.RestoreState(value, LoadPriority.ObjectProperty);
                }
            }
            
            // Finishing Touches -- Dependent on property loading
            foreach (SaveableEntity saveable in saveableEntities)
            {
                saveable.ApplyFinishingTouches();
            }
        }
        #endregion
    }
}
