using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;

namespace Frankie.Core
{
    public class FrankieDebugger : MonoBehaviour
    {
        // Cached References
        PlayerInput playerInput = null;
        GameObject saver = null;

        // Lazy Values
        ReInitLazyValue<SavingWrapper> savingWrapper = null;

        #region UnityMethods
        private void Awake()
        {
            // References
            playerInput = new PlayerInput();
            saver = GameObject.FindGameObjectWithTag("Saver");
            savingWrapper = new ReInitLazyValue<SavingWrapper>(SetupSavingWrapper);

            // Debug Hook-Ups
            playerInput.Debug.Save.performed += context => Save();
            playerInput.Debug.Load.performed += context => Continue();
            playerInput.Debug.Delete.performed += context => Delete();
        }

        private void Start()
        {
            savingWrapper.ForceInit();
        }

        private void OnEnable()
        {
            playerInput.Debug.Enable();
        }

        private void OnDisable()
        {
            playerInput.Debug.Disable();
        }
        #endregion

        #region SavingWrapperDebug
        private SavingWrapper SetupSavingWrapper()
        {
            if (saver == null) { saver = GameObject.FindGameObjectWithTag("Saver"); }
            return saver.GetComponent<SavingWrapper>();
        }

        private void Save()
        {
            savingWrapper.value.Save();
        }

        private void Continue()
        {
            savingWrapper.value.Continue();
        }

        private void Delete()
        {
            savingWrapper.value.Delete();
        }

        #endregion

    }
}
