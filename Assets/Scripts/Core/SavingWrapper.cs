using System.Collections;
using UnityEngine;
using Frankie.Saving;
using Frankie.ZoneManagement;

namespace Frankie.Core
{
    public class SavingWrapper : MonoBehaviour
    {
        const string defaultSaveFile = "save";
        const string defaultSessionFile = "session";
        [SerializeField] bool deleteSaveFileOnStart = false;
        [SerializeField] KeyCode saveKey = KeyCode.P;
        [SerializeField] KeyCode loadKey = KeyCode.L;
        [SerializeField] KeyCode deleteKey = KeyCode.Delete;

        IEnumerator FullLoadFromSave(string saveFile)
        {
            if (deleteSaveFileOnStart)
            {
                Delete();
                yield break;
            }
            Destroy(GameObject.FindGameObjectWithTag("Player")); // Player reconstructed after scene load (prevents control lock-up)
            yield return GetComponent<SavingSystem>().LoadLastScene(saveFile);

            SceneLoader sceneLoader = GameObject.FindGameObjectWithTag("SceneLoader").GetComponent<SceneLoader>();
            sceneLoader.SetCurrentZoneToCurrentScene();
            Fader fader = FindObjectOfType<Fader>();
            fader.UpdateFadeStateImmediate();
        }

        private void Update()
        {
            if (Input.GetKeyDown(loadKey))
            {
                Load();
            }
            if (Input.GetKeyDown(saveKey))
            {
                Save();
            }
            if (Input.GetKeyDown(deleteKey))
            {
                Delete();
            }
        }

        public void LoadSession()
        {
            GetComponent<SavingSystem>().LoadLastScene(defaultSessionFile);
        }

        private void Load()
        {
            StartCoroutine(FullLoadFromSave(defaultSaveFile));
        }

        public void SaveSession()
        {
            GetComponent<SavingSystem>().Save(defaultSessionFile);
        }

        public void Save()
        {
            GetComponent<SavingSystem>().CopySessionToSave(defaultSessionFile, defaultSaveFile);
        }

        public void Delete()
        {
            GetComponent<SavingSystem>().Delete(defaultSaveFile);
        }
    }

}
