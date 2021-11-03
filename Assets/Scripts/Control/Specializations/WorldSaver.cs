using Frankie.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control.Specialization
{
    public class WorldSaver : MonoBehaviour
    {
        // Cached References
        SavingWrapper savingWrapper = null;

        private void Start()
        {
            savingWrapper = GameObject.FindGameObjectWithTag("Saver").GetComponent<SavingWrapper>();
            // SceneLoader is a persistent object, thus can only be found after Awake -- so find in Start
        }

        public void Save()
        {
            savingWrapper.Save();
        }
    }
}