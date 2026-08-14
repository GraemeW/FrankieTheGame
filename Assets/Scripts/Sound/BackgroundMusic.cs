using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using LowDefMustard.Zones;
using Frankie.Saving;
using Frankie.Combat;
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
        [Header("Standard Fixed Audio")]
        [SerializeField] private AudioClip levelUpAudio;

        // State
        private AudioClip currentWorldMusic;
        private bool isWorldMusicLooping = true;
        private float worldMusicTimeIndex = 0f;

        // Cached References
        private AudioSource audioSource;
        private Coroutine musicFadeCoroutine;

        // Const
        private const float _minVolume = 0.0001f; // -80dB
        private const string _mixerVolumeReference = "backgroundVolume";
        
        #region Static
        private static IEnumerator StartFade(AudioMixerGroup audioMixerGroup, string exposedMixedVolumeReference, float duration, float targetVolume)
        {
            float currentTime = 0;
            audioMixerGroup.audioMixer.GetFloat(exposedMixedVolumeReference, out float currentVol);
            currentVol = Mathf.Pow(10, currentVol / 20);
            float targetValue = Mathf.Clamp(targetVolume, 0.0001f, 1);

            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                float newVol = Mathf.Lerp(currentVol, targetValue, currentTime / duration);
                audioMixerGroup.audioMixer.SetFloat(exposedMixedVolumeReference, Mathf.Log10(newVol) * 20);
                yield return null;
            }
        }
        #endregion

        #region UnityMethods
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            RefreshVolume();
        }

        private void Start()
        {
            if (CoreAudio.TryGetBackgroundAudioMixer(out AudioMixerGroup audioMixerGroup))
            {
                audioSource.outputAudioMixerGroup = audioMixerGroup;
            }
            
            Zone currentZone = SceneLoaderBase.GetCurrentZone();
            if (currentZone == null) { Debug.Log("Zone load failed");  return; }
            ConfigureNewWorldAudio(currentZone.GetZoneAudio(), currentZone.IsZoneAudioLooping(), true);
        }

        private void OnEnable()
        {
            RefreshVolume();
            SceneLoaderBase.zoneUpdated += ParseZoneUpdate;
            BattleEventBus<BattleStagingEvent>.SubscribeToEvent(HandleBattleStagingEvent);
        }

        private void OnDisable()
        {
            SceneLoaderBase.zoneUpdated -= ParseZoneUpdate;
            BattleEventBus<BattleStagingEvent>.UnsubscribeFromEvent(HandleBattleStagingEvent);
        }

        private void OnDestroy()
        {
            if (musicFadeCoroutine != null) { StopCoroutine(musicFadeCoroutine); }
        }
        #endregion

        #region PublicMethods
        public void RefreshVolume()
        {
            if (PlayerPrefsController.BackgroundVolumeKeyExists())
            {
                volume = Mathf.Clamp(PlayerPrefsController.GetBackgroundVolume(), _minVolume, 1f);
            }
            audioSource.volume = 1.0f; // Audio volume managed by mixer
            audioSource.outputAudioMixerGroup.audioMixer.SetFloat(_mixerVolumeReference, Mathf.Log10(volume) * 20);
        }
        #endregion

        #region Standard Transitions
        private IEnumerator TransitionToAudio(AudioClip audioClip, bool isLooping, float timeIndex = 0f)
        {
            if (audioClip == null) { yield break; }
            
            yield return StartFade(audioSource.outputAudioMixerGroup, _mixerVolumeReference, musicFadeDuration, 0f);
            audioSource.Stop();
            audioSource.clip = audioClip;
            audioSource.loop = isLooping;
            audioSource.time = Mathf.Clamp(timeIndex, 0f, audioClip.length - 0.01f);
            audioSource.Play();
            yield return StartFade(audioSource.outputAudioMixerGroup, _mixerVolumeReference, musicFadeDuration, volume);
        }

        private IEnumerator TransitionToAudioImmediate(AudioClip audioClip, bool isLooping)
        {
            audioSource.Stop();
            audioSource.clip = audioClip;
            audioSource.loop = isLooping;
            audioSource.time = 0f;
            audioSource.Play();
            yield return StartFade(audioSource.outputAudioMixerGroup, _mixerVolumeReference, musicFadeDuration, volume);
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
            currentWorldMusic = audioClip;
            isWorldMusicLooping = isLooping;
            
            if (BackgroundMusicOverride.TryGetOverrideAudio(out AudioClip overrideAudio))
            {
                OverrideMusic(overrideAudio);
                return;
            }
            
            if (musicFadeCoroutine != null) { StopCoroutine(musicFadeCoroutine); }
            musicFadeCoroutine = StartCoroutine(immediate ? TransitionToAudioImmediate(audioClip, isLooping) : TransitionToAudio(audioClip, isLooping));
        }
        #endregion

        #region BattleAudio
        private static AudioClip GetBattleAudioClip(IList<BattleEntity> battleEntities)
        {
            IList<CombatParticipant> viableCombatParticipants = CombatParticipant.GetPriorityCombatParticipants(battleEntities);

            int randomCombatParticipantIndex = Random.Range(0, viableCombatParticipants.Count);
            return viableCombatParticipants[randomCombatParticipantIndex].GetAudioClip();
        }

        private void SetBattleMusic(AudioClip audioClip)
        {
            if (audioClip == null) { return; }
            
            worldMusicTimeIndex = audioSource.time;
            
            if (musicFadeCoroutine != null) { StopCoroutine(musicFadeCoroutine); }
            musicFadeCoroutine = StartCoroutine(TransitionToAudio(audioClip, true));
        }

        private void StopBattleMusic()
        {
            if (musicFadeCoroutine != null) { StopCoroutine(musicFadeCoroutine); }
            
            if (BackgroundMusicOverride.TryGetOverrideAudio(out AudioClip overrideAudio))
            {
                OverrideMusic(overrideAudio);
                return;
            }
            
            if (musicFadeCoroutine != null) { StopCoroutine(musicFadeCoroutine); }
            musicFadeCoroutine = StartCoroutine(TransitionToAudio(currentWorldMusic, isWorldMusicLooping, worldMusicTimeIndex));
        }
        #endregion

        #region MusicOverrides
        public bool OverrideMusic(AudioClip audioClip)
        {
            if (audioClip == null) { return false; }
            worldMusicTimeIndex = audioSource.time;
            
            if (musicFadeCoroutine != null) { StopCoroutine(musicFadeCoroutine); }
            musicFadeCoroutine = StartCoroutine(TransitionToAudio(audioClip, true));
            return true;
        }

        public void StopOverrideMusic()
        {
            if (musicFadeCoroutine != null) { StopCoroutine(musicFadeCoroutine); }
            musicFadeCoroutine = StartCoroutine(TransitionToAudio(currentWorldMusic, isWorldMusicLooping, worldMusicTimeIndex));
        }
        #endregion
    }
}
