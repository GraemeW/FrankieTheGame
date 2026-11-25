using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;
using LowDefMustard.Core;
using LowDefMustard.Utils;
using LowDefMustard.Zones;
using Frankie.Saving;
using Frankie.Combat;
using Random = UnityEngine.Random;

namespace Frankie.Sound
{
    [RequireComponent(typeof(AudioSource))]
    public class BackgroundMusic : MonoBehaviour
    {
        // Notes:
        //  - Zone music loads through Addressables on each zone change (during the fade-out), so only the playing tracks stay resident
        //  - Override music loads the same way, on its own handle (released once the source is back on world music)
        //  - The replaced track's handle is released once the audio source has swapped off it ~ i.e. to avoid cutting off a streamed clip

        // Tunables
        [Header("Main Behaviour Configurables")]
        [SerializeField][Range(0f, 1.0f)] private float volume = 0.4f;
        [SerializeField] private float musicFadeDuration = 3.0f;
        [Header("Standard Fixed Audio")]
        [SerializeField] private AudioClip levelUpAudio;

        // State
        private readonly SwappableAssetHandle<AudioClip> worldMusicHandle = new();
        private readonly SwappableAssetHandle<AudioClip> overrideMusicHandle = new();
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
            if (currentZone == null) { LDMLog.Warning(LDMLogCategory.Sound, "No current zone found. Keeping the current background music."); return; }
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
            worldMusicHandle.ReleaseAll();
            overrideMusicHandle.ReleaseAll();
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
            yield return SwapAndFadeIn(audioClip, isLooping, timeIndex);
        }

        private IEnumerator TransitionToWorldMusic(float timeIndex = 0f, bool immediate = false)
        {
            yield return TransitionToLoadedMusic(worldMusicHandle.current, isWorldMusicLooping, timeIndex, immediate);
        }

        private IEnumerator TransitionToLoadedMusic(AsyncOperationHandle<AudioClip> musicLoad, bool isLooping, float timeIndex = 0f, bool immediate = false)
        {
            if (!musicLoad.IsValid()) { yield break; }
            
            if (!immediate) { yield return StartFade(audioSource.outputAudioMixerGroup, _mixerVolumeReference, musicFadeDuration, 0f); } // Track's load started in TryLoad (ConfigureNewWorldAudio / OverrideMusic), so it runs alongside the fade-out
            yield return musicLoad; // Expected complete by this point
            if (musicLoad.Status != AsyncOperationStatus.Succeeded || musicLoad.Result == null)
            {
                LDMLog.Warning(LDMLogCategory.Sound, $"Couldn't load the music ({musicLoad.OperationException?.Message}). Check it's an Addressables entry.", this);
                yield break;
            }
            yield return SwapAndFadeIn(musicLoad.Result, isLooping, timeIndex);
        }

        private IEnumerator SwapAndFadeIn(AudioClip audioClip, bool isLooping, float timeIndex)
        {
            audioSource.Stop();
            audioSource.clip = audioClip;
            audioSource.loop = isLooping;
            audioSource.time = Mathf.Clamp(timeIndex, 0f, audioClip.length - 0.01f);
            audioSource.Play();
            
            // Off the old tracks now
            worldMusicHandle.ReleaseReplaced();
            overrideMusicHandle.ReleaseReplaced();
            
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

        private void ConfigureNewWorldAudio(AssetReferenceAudioClip zoneAudio, bool isLooping, bool immediate = false)
        {
            if (!worldMusicHandle.TryLoad(zoneAudio)) { return; } // No zone music ~ keep what's playing
            isWorldMusicLooping = isLooping;
            
            if (BackgroundMusicOverride.TryGetOverrideAudio(out AssetReferenceAudioClip overrideAudio))
            {
                OverrideMusic(overrideAudio);
                return;
            }
            
            if (musicFadeCoroutine != null) { StopCoroutine(musicFadeCoroutine); }
            musicFadeCoroutine = StartCoroutine(TransitionToWorldMusic(0f, immediate));
        }
        #endregion

        #region BattleAudio
        private static AudioClip GetBattleAudioClip(IList<BattleEntity> battleEntities)
        {
            IList<CombatParticipant> viableCombatParticipants = CombatParticipant.GetPriorityCombatParticipants(battleEntities);
            if (viableCombatParticipants.Count == 0) { return null; }

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
            
            if (BackgroundMusicOverride.TryGetOverrideAudio(out AssetReferenceAudioClip overrideAudio))
            {
                OverrideMusic(overrideAudio);
                return;
            }
            
            if (musicFadeCoroutine != null) { StopCoroutine(musicFadeCoroutine); }
            musicFadeCoroutine = StartCoroutine(TransitionToWorldMusic(worldMusicTimeIndex));
        }
        #endregion

        #region MusicOverrides
        public bool OverrideMusic(AssetReferenceAudioClip overrideAudio)
        {
            if (!overrideMusicHandle.TryLoad(overrideAudio)) { return false; }
            worldMusicTimeIndex = audioSource.time;
            
            if (musicFadeCoroutine != null) { StopCoroutine(musicFadeCoroutine); }
            musicFadeCoroutine = StartCoroutine(TransitionToLoadedMusic(overrideMusicHandle.current, true));
            return true;
        }

        public void StopOverrideMusic()
        {
            overrideMusicHandle.RetireCurrent(); // Released once the source is back on world music
            if (musicFadeCoroutine != null) { StopCoroutine(musicFadeCoroutine); }
            musicFadeCoroutine = StartCoroutine(TransitionToWorldMusic(worldMusicTimeIndex));
        }
        #endregion
    }
}
