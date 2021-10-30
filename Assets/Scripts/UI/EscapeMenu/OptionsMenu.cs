using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Frankie.Settings;
using Frankie.Control;
using Frankie.Sound;

namespace Frankie.Speech.UI
{
    public class OptionsMenu : DialogueOptionBox
    {
        // Tunables
        [SerializeField] Slider masterVolumeSlider = null;
        [SerializeField] Slider backgroundVolumeSlider = null;
        [SerializeField] Slider soundEffectsVolumeSlider = null;
        [SerializeField] float defaultMasterVolume = 0.8f;
        [SerializeField] float defaultBackgroundVolume = 0.5f;
        [SerializeField] float defaultSoundEffectsVolume = 0.3f;

        // Cached References
        BackgroundMusic backgroundMusic = null;
        EscapeMenu escapeMenu = null;

        public override void Setup(string optionText)
        {
            InitializeSoundEffectsSliders();

            backgroundMusic = FindObjectOfType<BackgroundMusic>();
            // find in Start since persistent object, spawned during Awake
        }

        public void Setup(EscapeMenu escapeMenu)
        {
            this.escapeMenu = escapeMenu;
            if (gameObject.activeSelf) { SubscribeToEscapeMenu(true); }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToEscapeMenu(true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SubscribeToEscapeMenu(false);
        }

        private void SubscribeToEscapeMenu(bool enable)
        {
            if (escapeMenu == null) { return; }
            if (enable)
            {
                escapeMenu.escapeMenuItemSelected += Cancel;
            }
            else
            {
                escapeMenu.escapeMenuItemSelected -= Cancel;
            }
        }

        private void InitializeSoundEffectsSliders()
        {
            if (PlayerPrefsController.MasterVolumeKeyExists())
            {
                masterVolumeSlider.value = PlayerPrefsController.GetMasterVolume();
            }
            else
            {
                masterVolumeSlider.value = defaultMasterVolume;
            }

            if (PlayerPrefsController.BackgroundVolumeKeyExists())
            {
                backgroundVolumeSlider.value = PlayerPrefsController.GetBackgroundVolume();
            }
            else
            {
                backgroundVolumeSlider.value = defaultBackgroundVolume;
            }

            if (PlayerPrefsController.SoundEffectsVolumeKeyExists())
            {
                soundEffectsVolumeSlider.value = PlayerPrefsController.GetSoundEffectsVolume();
            }
            else
            {
                soundEffectsVolumeSlider.value = defaultSoundEffectsVolume;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (backgroundMusic != null)
            {
                float calculatedVolume = masterVolumeSlider.value * backgroundVolumeSlider.value;
                backgroundMusic.SetVolume(calculatedVolume);
            }
        }

        public void SaveAndExit()
        {
            PlayerPrefsController.SetMasterVolume(masterVolumeSlider.value);
            PlayerPrefsController.SetBackgroundVolume(backgroundVolumeSlider.value);
            PlayerPrefsController.SetSoundEffectsVolume(soundEffectsVolumeSlider.value);
            Destroy(gameObject);
        }

        public void Cancel()
        {
            HandleClientExit();
            Destroy(gameObject);
        }
    }

}
