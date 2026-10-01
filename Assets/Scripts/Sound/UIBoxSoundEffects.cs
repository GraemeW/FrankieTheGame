using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LowDefMustard.Control;
using LowDefMustard.UIBox;

namespace Frankie.Sound
{
    public class UIBoxSoundEffects : SoundEffects
    {
        // Tunables
        [SerializeField] private UIBoxBase uiBox;
        [SerializeField] private AudioSource textScanAudioSource;
        [SerializeField] private AudioClip textScanAudioClip;
        [SerializeField] private AudioClip chooseAudioClip;
        [SerializeField] private AudioClip highlightAudioClip;
        [SerializeField] private AudioClip enterClip;
        [SerializeField] private AudioClip exitClip;
        [SerializeField] private float textScanLoopDelay = 0.1f;

        // State
        private bool isTextScanActive = false;
        private Coroutine textScanCoroutine;

        #region UnityMethods
        protected override void OnEnable()
        {
            base.OnEnable();
            if (uiBox != null) { uiBox.SubscribeToReceiverUpdates(true, HandleDialogueBoxUpdate); }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (uiBox != null) { uiBox.SubscribeToReceiverUpdates(false, HandleDialogueBoxUpdate); } // Persistent copies outlive their box
            if (textScanCoroutine != null) { StopCoroutine(textScanCoroutine); }
        }

        protected override void InitializePersistentSoundEffect()
        {
            // Persistent copies play a single clip - detach from the source box's updates
            if (uiBox != null) { uiBox.SubscribeToReceiverUpdates(false, HandleDialogueBoxUpdate); }
            base.InitializePersistentSoundEffect();
        }
        
        protected override IEnumerable<AudioSource> GetDedicatedAudioSources()
        {
            yield return textScanAudioSource;
        }

        protected override void PreConfigureAudioSources()
        {
            base.PreConfigureAudioSources();
            if (textScanAudioSource == null) { return; }
            textScanAudioSource.clip = textScanAudioClip;
            textScanAudioSource.time = 0f;
        }
        #endregion

        #region EventHandlers
        private void HandleDialogueBoxUpdate(ReceiverModifiedType receiverModifiedType, ReceiverModifiedData uiBoxModifiedData)
        {
            switch (receiverModifiedType)
            {
                case ReceiverModifiedType.WritingStateChanged:
                    ConfigureTextScanAudio(uiBoxModifiedData.writingState);
                    break;
                case ReceiverModifiedType.ItemSelected:
                    PlayClipAfterDestroy(chooseAudioClip); // Selection often destroys the box
                    break;
                case ReceiverModifiedType.ItemHighlighted:
                    PlayClip(highlightAudioClip);
                    break;
                case ReceiverModifiedType.ClientEnter:
                    PlayClip(enterClip);
                    break;
                case ReceiverModifiedType.ClientExit:
                    PlayClipAfterDestroy(exitClip);
                    break;
            }
        }
        #endregion

        #region PrivateMethods
        private void ConfigureTextScanAudio(bool enable)
        {
            if (textScanAudioSource == null) { return; }
            if (enable)
            {
                InitializeVolume();
                textScanAudioSource.clip = textScanAudioClip;
                isTextScanActive = true;
                
                if (textScanCoroutine != null) { StopCoroutine(textScanCoroutine); }
                textScanCoroutine = StartCoroutine(QueueTextScanAudio());
            }
            else
            {
                isTextScanActive = false;
                textScanAudioSource.Stop();
            }
        }

        private IEnumerator QueueTextScanAudio()
        {
            while (isTextScanActive)
            {
                if (!textScanAudioSource.isPlaying) { textScanAudioSource.Play(); }
                yield return new WaitForSeconds(textScanLoopDelay);
            }
        }
        #endregion
    }
}
