using Frankie.Combat;
using Frankie.ZoneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Frankie.Core
{
    public class BackgroundMusic : MonoBehaviour
    {
        // Tunables
        [SerializeField] float volume = 0.2f;
        [SerializeField] float musicFadeDuration = 3.0f;
        [SerializeField] AudioMixer audioMixer = null;

        // State
        AudioClip currentWorldMusic = null;
        bool isWorldMusicLooping = true;
        float worldMusicTimeIndex = 0f;
        bool isBattleMusic = false;

        // Cached References
        AudioSource audioSource = null;
        PlayerStateHandler playerStateHandler = null;
        SceneLoader sceneLoader = null;

        // Static Functions
        private static string MIXER_VOLUME_REFERENCE = "backgroundVolume";

        // Public Functions
        public void SetVolume(float volume)
        {
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

        // Private Functions
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            audioSource.volume = volume;

            // Note:  Cached references obtained in Start
            // Persistent objects not reliable to attempt to get within Awake -- hence fancy footwork on OnEnable/Disable
            SetUpSceneLoader();
            AttemptToSetUpPlayerReference();
        }

        private void OnEnable()
        {
            if (playerStateHandler != null)
            {
                playerStateHandler.playerStateChanged += ParsePlayerState;
            }
            if (sceneLoader != null)
            {
                if (playerStateHandler == null) { sceneLoader.zoneUpdated += AttemptToSetUpPlayerReference; }
                sceneLoader.zoneUpdated += ParseZoneUpdate;
            }
        }

        private void OnDisable()
        {
            if (playerStateHandler != null)
            {
                playerStateHandler.playerStateChanged -= ParsePlayerState;
            }
            if (sceneLoader != null)
            {
                if (playerStateHandler == null) { sceneLoader.zoneUpdated -= AttemptToSetUpPlayerReference; }
                sceneLoader.zoneUpdated -= ParseZoneUpdate;
            }
        }

        private void SetUpSceneLoader()
        {
            sceneLoader = GameObject.FindGameObjectWithTag("SceneLoader").GetComponent<SceneLoader>();
            sceneLoader.zoneUpdated += AttemptToSetUpPlayerReference;
            sceneLoader.zoneUpdated += ParseZoneUpdate;

            Zone currentZone = sceneLoader.GetCurrentZone();
            ConfigureNewWorldAudio(currentZone.GetZoneAudio(), currentZone.IsZoneAudioLooping(), true);
        }

        private void AttemptToSetUpPlayerReference()
        {
            // Player object is not always present, e.g. on intro splash screens
            // Special handling to check for existence on each scene load until found
            if (playerStateHandler == null)
            {
                GameObject playerGameObject = GameObject.FindGameObjectWithTag("Player");
                if (playerGameObject != null)
                {
                    playerStateHandler = playerGameObject.GetComponent<PlayerStateHandler>();
                    playerStateHandler.playerStateChanged += ParsePlayerState;
                    sceneLoader.zoneUpdated -= AttemptToSetUpPlayerReference;
                }
            }
        }

        private void AttemptToSetUpPlayerReference(Zone zone)
        {
            AttemptToSetUpPlayerReference();
        }

        private void ParsePlayerState(PlayerState playerState)
        {
            if (playerState == PlayerState.inBattle)
            {
                BattleController battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();
                List<CombatParticipant> combatParticipants = battleController.GetEnemies();
                List<AudioClip> audioClipOptions = new List<AudioClip>();
                foreach (CombatParticipant combatParticipant in combatParticipants)
                {
                    if (combatParticipant.GetAudioClip() != null)
                    {
                        audioClipOptions.Add(combatParticipant.GetAudioClip());
                    }
                }
                if (audioClipOptions.Count == 0) { return; }

                int randomAudioClipIndex = Random.Range(0, audioClipOptions.Count);
                AudioClip combatAudio = audioClipOptions[randomAudioClipIndex];
                SetBattleMusic(combatAudio);
            }
            else if(isBattleMusic && playerState == PlayerState.inWorld)
            {
                StopBattleMusic();
            }
        }

        private void ParseZoneUpdate(Zone zone)
        {
            ConfigureNewWorldAudio(zone.GetZoneAudio(), zone.IsZoneAudioLooping());
        }

        private void ConfigureNewWorldAudio(AudioClip audioClip, bool isLooping, bool immediate = false)
        {
            if (audioClip == null) { return; }

            if (immediate) { StartCoroutine(TransitionToAudioImmediate(audioClip, isLooping)); }
            else { StartCoroutine(TransitionToAudio(audioClip, isLooping)); }
            currentWorldMusic = audioClip;
            isWorldMusicLooping = isLooping;
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

    }

}
