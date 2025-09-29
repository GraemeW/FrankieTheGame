using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Frankie.Saving;
using Frankie.Control;
using Frankie.Combat;
using Frankie.ZoneManagement;

namespace Frankie.Sound
{
    [RequireComponent(typeof(AudioSource))]
    public class BackgroundMusic : MonoBehaviour
    {
        // Tunables
        [Header("Main Behaviour Configurables")]
        [SerializeField][Range(0f, 1.0f)] float volume = 0.4f;
        [SerializeField] float musicFadeDuration = 3.0f;
        [SerializeField] AudioMixer audioMixer = null;
        [Header("Standard Fixed Audio")]
        [SerializeField] AudioClip levelUpAudio = null;

        // State
        AudioClip currentWorldMusic = null;
        bool isWorldMusicLooping = true;
        float worldMusicTimeIndex = 0f;
        bool wasMusicOverriddenOnStart = false;
        bool isBattleMusic = false;

        // Cached References
        AudioSource audioSource = null;

        // Static Variables
        private static string MIXER_VOLUME_REFERENCE = "masterVolume";

        #region UnityMethods
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            Zone currentZone = SceneLoader.GetCurrentZone();
            if (currentZone == null) { UnityEngine.Debug.Log("Zone load failed");  return; }
            ConfigureNewWorldAudio(currentZone.GetZoneAudio(), currentZone.IsZoneAudioLooping(), true);
        }

        private void OnEnable()
        {
            InitializeVolume();

            SceneLoader.zoneUpdated += ParseZoneUpdate;
            BattleEventBus<BattleEnterEvent>.SubscribeToEvent(ParseBattleEntry);
            BattleEventBus<BattleExitEvent>.SubscribeToEvent(ParseBattleExit);
        }

        private void OnDisable()
        {
            SceneLoader.zoneUpdated -= ParseZoneUpdate;
            BattleEventBus<BattleEnterEvent>.UnsubscribeFromEvent(ParseBattleEntry);
            BattleEventBus<BattleExitEvent>.UnsubscribeFromEvent(ParseBattleExit);
        }
        #endregion

        #region PublicMethods
        public void SetVolume(float volume)
        {
            volume = Mathf.Clamp(volume, 0f, 1.0f);
            this.volume = volume;
            audioSource.volume = volume;
        }

        public static IEnumerator StartFade(AudioMixer audioMixer, string exposedMixedVolumeReference, float duration, float targetVolume)
        {
            float currentTime = 0;
            float currentVol;
            audioMixer.GetFloat(exposedMixedVolumeReference, out currentVol);
            currentVol = Mathf.Pow(10, currentVol / 20);
            float targetValue = Mathf.Clamp(targetVolume, 0.0001f, 1);

            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                float newVol = Mathf.Lerp(currentVol, targetValue, currentTime / duration);
                audioMixer.SetFloat(exposedMixedVolumeReference, Mathf.Log10(newVol) * 20);
                yield return null;
            }
            yield break;
        }
        #endregion

        #region Initialization
        private void InitializeVolume()
        {
            if (PlayerPrefsController.MasterVolumeKeyExists())
            {
                if (PlayerPrefsController.BackgroundVolumeKeyExists())
                {
                    volume = PlayerPrefsController.GetMasterVolume() * PlayerPrefsController.GetBackgroundVolume();
                }
                else
                {
                    volume = PlayerPrefsController.GetMasterVolume();
                }
            }
            audioSource.volume = volume;
        }
        #endregion

        #region Standard Transitions
        private IEnumerator TransitionToAudio(AudioClip audioClip, bool isLooping, float timeIndex = 0f)
        {
            yield return StartFade(audioMixer, MIXER_VOLUME_REFERENCE, musicFadeDuration, 0f);
            audioSource.Stop();
            audioSource.clip = audioClip;
            audioSource.loop = isLooping;
            audioSource.time = timeIndex;
            audioSource.Play();
            yield return StartFade(audioMixer, MIXER_VOLUME_REFERENCE, musicFadeDuration, volume);
        }

        private IEnumerator TransitionToAudioImmediate(AudioClip audioClip, bool isLooping)
        {
            audioSource.Stop();
            audioSource.clip = audioClip;
            audioSource.loop = isLooping;
            audioSource.time = 0f;
            audioSource.Play();
            yield return StartFade(audioMixer, MIXER_VOLUME_REFERENCE, musicFadeDuration, volume);
        }
        #endregion

        #region MessageHandling
        private void ParseBattleEntry(BattleEnterEvent battleStartedEvent)
        {
            AudioClip audioClip = GetBattleAudioClip(battleStartedEvent.enemyEntities);
            SetBattleMusic(audioClip);
            BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(ParseBattleState);
        }

        private void ParseBattleExit(BattleExitEvent battleExitEvent)
        {
            if (isBattleMusic)
            {
                StopBattleMusic();
            }
            BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(ParseBattleState);
        }

        private void ParseBattleState(BattleStateChangedEvent battleStateChangedEvent)
        {
            if (battleStateChangedEvent.battleState == BattleState.Rewards)
            {
                SetBattleMusic(levelUpAudio);
            }
        }
        #endregion

        #region WorldAudio
        private void ParseZoneUpdate(Zone zone)
        {
            ConfigureNewWorldAudio(zone.GetZoneAudio(), zone.IsZoneAudioLooping());
        }

        private void ConfigureNewWorldAudio(AudioClip audioClip, bool isLooping, bool immediate = false)
        {
            if (audioClip == null) { return; }

            if (!wasMusicOverriddenOnStart)
            {
                if (immediate) { StartCoroutine(TransitionToAudioImmediate(audioClip, isLooping)); }
                else { StartCoroutine(TransitionToAudio(audioClip, isLooping)); }
            }
            currentWorldMusic = audioClip;
            isWorldMusicLooping = isLooping;
        }
        #endregion

        #region BattleAudio
        private AudioClip GetBattleAudioClip(List<BattleEntity> battleEntities)
        {
            List<AudioClip> audioClipOptions = new List<AudioClip>();
            foreach (BattleEntity battleEntity in battleEntities)
            {
                if (battleEntity.combatParticipant.GetAudioClip() != null)
                {
                    audioClipOptions.Add(battleEntity.combatParticipant.GetAudioClip());
                }
            }
            if (audioClipOptions.Count == 0) { return null; }

            int randomAudioClipIndex = Random.Range(0, audioClipOptions.Count);
            AudioClip combatAudio = audioClipOptions[randomAudioClipIndex];

            return combatAudio;
        }

        private void SetBattleMusic(AudioClip audioClip)
        {
            if (audioClip == null) { return; }

            isBattleMusic = true;
            worldMusicTimeIndex = audioSource.time;
            StartCoroutine(TransitionToAudio(audioClip, true));
        }

        private void StopBattleMusic()
        {
            isBattleMusic = false;
            StartCoroutine(TransitionToAudio(currentWorldMusic, isWorldMusicLooping, worldMusicTimeIndex));
        }
        #endregion

        #region MusicOverrides
        public void OverrideMusic(AudioClip audioClip, bool calledInStart = false)
        {
            if (audioClip == null) { return; }

            if (calledInStart) { wasMusicOverriddenOnStart = true; }
            else { worldMusicTimeIndex = audioSource.time; }

            worldMusicTimeIndex = audioSource.time;
            StartCoroutine(TransitionToAudio(audioClip, true));
        }

        public void StopOverrideMusic()
        {
            StartCoroutine(TransitionToAudio(currentWorldMusic, isWorldMusicLooping, worldMusicTimeIndex));
        }
        #endregion
    }
}
