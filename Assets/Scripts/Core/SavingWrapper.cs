using System.Collections;
using UnityEngine;
using Frankie.Saving;
using Frankie.ZoneManagement;

namespace Frankie.Core
{
    public class SavingWrapper : MonoBehaviour
    {
        const string defaultSaveFile = "save";
        [SerializeField] bool deleteSaveFileOnStart = false;
        [SerializeField] KeyCode saveKey = KeyCode.P;
        [SerializeField] KeyCode loadKey = KeyCode.L;
        [SerializeField] KeyCode deleteKey = KeyCode.Delete;

        private void Awake()
        {
            StartCoroutine(LoadLastScene());
        }

        IEnumerator LoadLastScene()
        {
            if (deleteSaveFileOnStart)
            {
                Delete();
                yield break;
            }
            yield return GetComponent<SavingSystem>().LoadLastScene(defaultSaveFile);

            Fader fader = FindObjectOfType<Fader>();
            fader.UpdateFadeStateImmediate();
        }

        private void Update()
        {
            if (Input.GetKeyDown(loadKey))
            {
                LoadFull();
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

        public void Load()
        {
            GetComponent<SavingSystem>().Load(defaultSaveFile);
        }

        private void LoadFull()
        {
            StartCoroutine(LoadLastScene());
        }

        public void Save()
        {
            GetComponent<SavingSystem>().Save(defaultSaveFile);
        }

        public void Delete()
        {
            GetComponent<SavingSystem>().Delete(defaultSaveFile);
        }
    }

}
