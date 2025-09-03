using UnityEngine;
using Frankie.Settings;
using Frankie.Sound;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    public class OptionsMenu : UIBox
    {
        // Tunables
        [Header("Hookups")]
        [SerializeField] UIChoiceSlider masterVolumeSlider = null;
        [SerializeField] UIChoiceSlider backgroundVolumeSlider = null;
        [SerializeField] UIChoiceSlider soundEffectsVolumeSlider = null;
        [SerializeField] SoundEffects soundUpdateConfirmEffect = null;

        [Header("Sound Settings")]
        [SerializeField] float defaultMasterVolume = 0.8f;
        [SerializeField] float defaultBackgroundVolume = 0.5f;
        [SerializeField] float defaultSoundEffectsVolume = 0.3f;

        // Cached References
        BackgroundMusic backgroundMusic = null;
        EscapeMenu escapeMenu = null;

        private void Start()
        {
            InitializeSoundEffectsSliders();
            InitializeResolutions();

            backgroundMusic = FindAnyObjectByType<BackgroundMusic>();
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
            if (PlayerPrefsController.MasterVolumeKeyExists()) { masterVolumeSlider.SetSliderValue(PlayerPrefsController.GetMasterVolume()); }
            else { masterVolumeSlider.SetSliderValue(defaultMasterVolume); }
            masterVolumeSlider.AddOnValueChangeListener((float _) => { ConfirmSoundUpdate(true); });

            if (PlayerPrefsController.BackgroundVolumeKeyExists()) { backgroundVolumeSlider.SetSliderValue(PlayerPrefsController.GetBackgroundVolume()); }
            else { backgroundVolumeSlider.SetSliderValue(defaultBackgroundVolume); }
            backgroundVolumeSlider.AddOnValueChangeListener((float _) => { ConfirmSoundUpdate(false); });

            if (PlayerPrefsController.SoundEffectsVolumeKeyExists()) { soundEffectsVolumeSlider.SetSliderValue(PlayerPrefsController.GetSoundEffectsVolume()); }
            else { soundEffectsVolumeSlider.SetSliderValue(defaultSoundEffectsVolume); }
            soundEffectsVolumeSlider.AddOnValueChangeListener((float _) => { ConfirmSoundUpdate(true); });
        }

        private void InitializeResolutions()
        {
            UnityEngine.Debug.Log($"Current Resolution: {Screen.currentResolution.width} x {Screen.currentResolution.height} @ {Screen.currentResolution.refreshRateRatio}Hz");
        }

        private void ConfirmSoundUpdate(bool playSoundEffect)
        {
            float calculatedVolume = masterVolumeSlider.GetSliderValue() * backgroundVolumeSlider.GetSliderValue();
            backgroundMusic?.SetVolume(calculatedVolume);

            if (playSoundEffect && soundUpdateConfirmEffect != null)
            {
                WriteVolumeToPlayerPrefs();
                soundUpdateConfirmEffect.PlayClip();
            }
        }

        private void WriteVolumeToPlayerPrefs()
        {
            PlayerPrefsController.SetMasterVolume(masterVolumeSlider.GetSliderValue());
            PlayerPrefsController.SetBackgroundVolume(backgroundVolumeSlider.GetSliderValue());
            PlayerPrefsController.SetSoundEffectsVolume(soundEffectsVolumeSlider.GetSliderValue());
        }

        public void SaveAndExit()
        {
            WriteVolumeToPlayerPrefs();
            PlayerPrefsController.SaveToDisk();
            Destroy(gameObject);
        }

        public void Cancel()
        {
            HandleClientExit();
            Destroy(gameObject);
        }
    }

}
