using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Frankie.Settings;
using Frankie.Core;
using Frankie.Control;

namespace Frankie.Speech.UI
{
    public class MenuOptions : DialogueOptionBox
    {
        // Tunables
        [SerializeField] Slider volumeSlider = null;
        [SerializeField] float defaultVolume = 0.4f;

        // Cached References
        BackgroundMusic backgroundMusic = null;

        protected override void Start()
        {
            if (PlayerPrefsController.VolumeKeyExist())
            {
                volumeSlider.value = PlayerPrefsController.GetMasterVolume();
            }
            else
            {
                volumeSlider.value = defaultVolume;
            }

            backgroundMusic = FindObjectOfType<BackgroundMusic>();
                // find in Start since persistent object, spawned during Awake
        }

        protected override void Update()
        {
            if (backgroundMusic != null)
            {
                backgroundMusic.SetVolume(volumeSlider.value);
            }
        }

        public void SaveAndExit()
        {
            PlayerPrefsController.SetMasterVolume(volumeSlider.value);
            Destroy(gameObject);
        }

        public void Cancel()
        {
            Destroy(gameObject);
        }

        public override void HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (ShowCursorOnAnyInteraction(playerInputType)) { return; }
            if (PrepareChooseAction(playerInputType)) { return; }
            if (MoveCursor(playerInputType)) { return; }
        }
    }

}
