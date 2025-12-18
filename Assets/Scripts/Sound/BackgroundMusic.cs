using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using Frankie.Saving;
using Frankie.Combat;
using Frankie.ZoneManagement;
using Random = UnityEngine.Random;

namespace Frankie.Sound
{
    [RequireComponent(typeof(AudioSource))]
    public class BackgroundMusic : MonoBehaviour
    {
        // Tunables
        [Header("Main Behaviour Configurables")]
        [SerializeField][Range(0f, 1.0f)] private float volume = 0.4f;
        [SerializeField] private float musicFadeDuration = 3.0f;
        [SerializeField] private AudioMixer audioMixer;
        [Header("Standard Fixed Audio")]
        [SerializeField] private AudioClip levelUpAudio;

        // State
        private AudioClip currentWorldMusic;
        private bool isWorldMusicLooping = true;
        private float worldMusicTimeIndex = 0f;
        private bool wasMusicOverriddenOnStart = false;

        // Cached References
        private AudioSource audioSource;

        #region Static
        private const string _mixerVolumeReference = "masterVolume";
        private const string _backgroundMusicTag = "BackgroundMusic";

        public static BackgroundMusic FindBackgroundMusic()
        {
            var backgroundMusicGameObject = GameObject.FindGameObjectWithTag(_backgroundMusicTag);
            return backgroundMusicGameObject != null ? backgroundMusicGameObject.GetComponent<BackgroundMusic>() : null;
        }
        
        private static IEnumerator StartFade(AudioMixer audioMixer, string exposedMixedVolumeReference, float duration, float targetVolume)
        {
            float currentTime = 0;
            audioMixer.GetFloat(exposedMixedVolumeReference, out float currentVol);
            currentVol = Mathf.Pow(10, currentVol / 20);
            float targetValue = Mathf.Clamp(targetVolume, 0.0001f, 1);

            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                float newVol = Mathf.Lerp(currentVol, targetValue, currentTime / duration);
                audioMixer.SetFloat(exposedMixedVolumeReference, Mathf.Log10(newVol) * 20);
                yield return null;
            }
        }
        #endregion

        #region UnityMethods
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            Zone currentZone = SceneLoader.GetCurrentZone();
            if (currentZone == null) { Debug.Log("Zone load failed");  return; }
            ConfigureNewWorldAudio(currentZone.GetZoneAudio(), currentZone.IsZoneAudioLooping(), true);
        }

        private void OnEnable()
        {
            InitializeVolume();

            SceneLoader.zoneUpdated += ParseZoneUpdate;
            BattleEventBus<BattleStagingEvent>.SubscribeToEvent(HandleBattleStagingEvent);
        }

        private void OnDisable()
        {
            SceneLoader.zoneUpdated -= ParseZoneUpdate;
            BattleEventBus<BattleStagingEvent>.UnsubscribeFromEvent(HandleBattleStagingEvent);
        }
        #endregion

        #region PublicMethods
        public void SetVolume(float setVolume)
        {
            setVolume = Mathf.Clamp(setVolume, 0f, 1.0f);
            volume = setVolume;
            audioSource.volume = setVolume;
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
            yield return StartFade(audioMixer, _mixerVolumeReference, musicFadeDuration, 0f);
            audioSource.Stop();
            audioSource.clip = audioClip;
            audioSource.loop = isLooping;
            audioSource.time = timeIndex;
            audioSource.Play();
            yield return StartFade(audioMixer, _mixerVolumeReference, musicFadeDuration, volume);
        }

        private IEnumerator TransitionToAudioImmediate(AudioClip audioClip, bool isLooping)
        {
            audioSource.Stop();
            audioSource.clip = audioClip;
            audioSource.loop = isLooping;
            audioSource.time = 0f;
            audioSource.Play();
            yield return StartFade(audioMixer, _mixerVolumeReference, musicFadeDuration, volume);
        }
        #endregion

        #region MessageHandling
        private void HandleBattleStagingEvent(BattleStagingEvent battleStagingEvent)
        {
            switch (battleStagingEvent.battleStagingType)
            {
                case BattleStagingType.BattleSetUp:
                {
                    if (battleStagingEvent.optionalParametersSet)
                    {
                        AudioClip audioClip = GetBattleAudioClip(battleStagingEvent.GetEnemyEntities());
                        SetBattleMusic(audioClip);
                    }
                    BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(HandleBattleStateChangedEvent);
                    break;
                }
                case BattleStagingType.BattleControllerPrimed:
                {
                    break;
                }
                case BattleStagingType.BattleTornDown:
                {
                    StopBattleMusic();
                    BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(HandleBattleStateChangedEvent);
                    break;
                }
            }
            

        }

        private void HandleBattleStateChangedEvent(BattleStateChangedEvent battleStateChangedEvent)
        {
            if (battleStateChangedEvent.battleState == BattleState.Rewards) { SetBattleMusic(levelUpAudio); }
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
                StartCoroutine(immediate
                    ? TransitionToAudioImmediate(audioClip, isLooping)
                    : TransitionToAudio(audioClip, isLooping));
            }
            currentWorldMusic = audioClip;
            isWorldMusicLooping = isLooping;
        }
        #endregion

        #region BattleAudio
        private AudioClip GetBattleAudioClip(IList<BattleEntity> battleEntities)
        {
            var audioClipOptions = (from battleEntity in battleEntities where battleEntity.combatParticipant.GetAudioClip() != null select battleEntity.combatParticipant.GetAudioClip()).ToList();
            if (audioClipOptions.Count == 0) { return null; }

            int randomAudioClipIndex = Random.Range(0, audioClipOptions.Count);
            AudioClip combatAudio = audioClipOptions[randomAudioClipIndex];

            return combatAudio;
        }

        private void SetBattleMusic(AudioClip audioClip)
        {
            if (audioClip == null) { return; }
            
            worldMusicTimeIndex = audioSource.time;
            StartCoroutine(TransitionToAudio(audioClip, true));
        }

        private void StopBattleMusic()
        {
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
