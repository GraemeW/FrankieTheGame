using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Frankie.Saving
{
    public class SavingSystem : MonoBehaviour
    {
        const string SAVE_FILE_EXTENSION = ".sav";
        const string SAVE_LAST_SCENE_BUILD_INDEX = "lastSceneBuildIndex";

        private static List<SaveableEntity> GetAllSaveableEntities()
        {
            List<SaveableEntity> saveableEntities = FindObjectsOfType<SaveableEntity>().ToList();
            foreach (SaveableRoot saveableRoot in FindObjectsOfType<SaveableRoot>()) // Captures inactive game objects
            {
                List<SaveableEntity> rootSaveableEntities = saveableRoot.gameObject.GetComponentsInChildren<SaveableEntity>(true).ToList();
                List<SaveableEntity> combinedSaveableEntities = saveableEntities.Union(rootSaveableEntities).ToList();
                saveableEntities = combinedSaveableEntities;
            }

            return saveableEntities;
        }

        public IEnumerator LoadLastScene(string saveFile)
        {
            JObject state = LoadFile(saveFile);
            string sceneName = SceneManager.GetActiveScene().name;
            if (state.ContainsKey(SAVE_LAST_SCENE_BUILD_INDEX))
            {
                string trySceneName = state[SAVE_LAST_SCENE_BUILD_INDEX].ToObject<string>();
                if (!string.IsNullOrWhiteSpace(trySceneName)) { sceneName = trySceneName; }
            }
            yield return SceneManager.LoadSceneAsync(sceneName); 
                // Note:  will throw scene existence error if saved scene name does not exist
                // i.e. scene name changes -> will corrupt save on those scenes -- so don't do it, or prepare for consequences
            RestoreState(state);
        }

        public void LoadWithinScene(string saveFile)
        {
            JObject state = LoadFile(saveFile);
            RestoreState(state);
        }

        public void Save(string saveFile)
        {
            JObject state = LoadFile(saveFile);
            CaptureState(state);
            SaveFile(saveFile, state);
        }

        public void CopySessionToSave(string sessionFile, string saveFile)
        {
            JObject state = LoadFile(sessionFile);
            CaptureState(state);
            SaveFile(saveFile, state);
        }

        public void Delete(string saveFile)
        {
            File.Delete(GetPathFromSaveFile(saveFile));
        }

        public IEnumerable<string> ListSaves()
        {
            List<string> saveFiles = Directory.EnumerateFiles(Application.persistentDataPath).ToList();
            foreach (string path in saveFiles)
            {
                if (Path.GetExtension(path) == SAVE_FILE_EXTENSION)
                {
                    yield return Path.GetFileNameWithoutExtension(path);
                }
            }
        }

        private JObject LoadFile(string saveFile)
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
                using (JsonTextReader reader = new JsonTextReader(textReader))
                {
                    reader.FloatParseHandling = FloatParseHandling.Double;

                    return JObject.Load(reader);
                }
            }
        }

        private void SaveFile(string saveFile, JObject state)
        {
            string path = GetPathFromSaveFile(saveFile);
            print("Saving to " + path);

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
                using (JsonTextWriter writer = new JsonTextWriter(textWriter))
                {
                    writer.Formatting = Formatting.Indented;
                    state.WriteTo(writer);
                }
            }
        }

        private void CaptureState(JObject state)
        {
            List<SaveableEntity> saveableEntities = GetAllSaveableEntities();

            foreach (SaveableEntity saveable in saveableEntities)
            {
                state[saveable.GetUniqueIdentifier()] = saveable.CaptureState();
            }

            state[SAVE_LAST_SCENE_BUILD_INDEX] = SceneManager.GetActiveScene().name;
        }

        private void RestoreState(JObject state)
        {
            // First Pass -- Object instantiation
            List<SaveableEntity> saveableEntities = GetAllSaveableEntities();
            foreach (SaveableEntity saveable in saveableEntities)
            {
                string id = saveable.GetUniqueIdentifier();
                if (state.ContainsKey(id))
                {
                    saveable.RestoreState(state[id], LoadPriority.ObjectInstantiation);
                }
            }

            // Second Pass -- Property loading
            saveableEntities = GetAllSaveableEntities();
            foreach (SaveableEntity saveable in saveableEntities)
            {
                string id = saveable.GetUniqueIdentifier();
                if (state.ContainsKey(id))
                {
                    saveable.RestoreState(state[id], LoadPriority.ObjectProperty);
                }
            }
        }

        private string GetPathFromSaveFile(string saveFile)
        {
            return Path.Combine(Application.persistentDataPath, saveFile + ".sav");
        }
    }
}
