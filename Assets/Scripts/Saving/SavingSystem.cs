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
            Dictionary<string, object> state = LoadFile(saveFile);
            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            if (state.ContainsKey("lastSceneBuildIndex"))
            {
                buildIndex = (int)state["lastSceneBuildIndex"];
            }
            yield return SceneManager.LoadSceneAsync(buildIndex);
            RestoreState(state);
        }

        public void LoadWithinScene(string saveFile)
        {
            Dictionary<string, object> state = LoadFile(saveFile);
            RestoreState(state);
        }

        public void Save(string saveFile)
        {
            Dictionary<string, object> state = LoadFile(saveFile);
            CaptureState(state);
            SaveFile(saveFile, state);
        }

        public void CopySessionToSave(string sessionFile, string saveFile)
        {
            Dictionary<string, object> state = LoadFile(sessionFile);
            CaptureState(state);
            SaveFile(saveFile, state);
        }

        public void Delete(string saveFile)
        {
            File.Delete(GetPathFromSaveFile(saveFile));
        }

        private Dictionary<string, object> LoadFile(string saveFile)
        {
            string path = GetPathFromSaveFile(saveFile);
            if (!File.Exists(path))
            {
                return new Dictionary<string, object>();
            }
            using (FileStream stream = File.Open(path, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (Dictionary<string, object>)formatter.Deserialize(stream);
            }
        }

        private void SaveFile(string saveFile, object state)
        {
            string path = GetPathFromSaveFile(saveFile);
            print("Saving to " + path);
            using (FileStream stream = File.Open(path, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, state);
            }
        }

        private void CaptureState(Dictionary<string, object> state)
        {
            List<SaveableEntity> saveableEntities = GetAllSaveableEntities();

            foreach (SaveableEntity saveable in saveableEntities)
            {
                state[saveable.GetUniqueIdentifier()] = saveable.CaptureState();
            }

            state["lastSceneBuildIndex"] = SceneManager.GetActiveScene().buildIndex;
        }



        private void RestoreState(Dictionary<string, object> state)
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
