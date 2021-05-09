using System.Collections;
using UnityEngine;
using Frankie.Saving;
using Frankie.ZoneManagement;

namespace Frankie.Core
{
    public class SavingWrapper : MonoBehaviour
    {
        // Tunables
        const string defaultSaveFile = "save";
        const string defaultSessionFile = "session";
        [SerializeField] bool deleteSaveFileOnStart = false;

        // Cached References
        PlayerInput playerInput = null;

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
            playerInput.Debug.Load.performed += context => Load();
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

        public void LoadSession()
        {
            GetComponent<SavingSystem>().LoadWithinScene(defaultSessionFile);
        }

        private void Load()
        {
            StartCoroutine(LoadFromSave(defaultSaveFile));
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
