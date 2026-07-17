using System.Collections;
using UnityEngine;
using Frankie.Control;
using Frankie.Utils.UI;

namespace Frankie.Sound
{
    public class UIBoxSoundEffects : SoundEffects
    {
        // Tunables
        [SerializeField] private UIBox uiBox;
        [SerializeField] private AudioClip textScanAudioClip;
        [SerializeField] private AudioClip chooseAudioClip;
        [SerializeField] private AudioClip enterClip;
        [SerializeField] private AudioClip exitClip;
        [SerializeField] private float textScanLoopDelay = 0.1f;

        // State
        private bool isTextScanActive = false;

        #region UnityMethods
        private void Start()
        {
            audioSource.Stop();
            audioSource.clip = textScanAudioClip;
            audioSource.time = 0f;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            uiBox.SubscribeToReceiverUpdates(true, HandleDialogueBoxUpdate);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            uiBox.SubscribeToReceiverUpdates(false, HandleDialogueBoxUpdate);
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
                    PlayClip(chooseAudioClip);
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
            if (enable)
            {
                InitializeVolume();
                audioSource.clip = textScanAudioClip;
                isTextScanActive = true;
                StartCoroutine(QueueTextScanAudio());
            }
            else
            {
                isTextScanActive = false;
                audioSource.Stop();
            }
        }

        private IEnumerator QueueTextScanAudio()
        {
            while (isTextScanActive)
            {
                if (!audioSource.isPlaying) { audioSource.Play(); }
                yield return new WaitForSeconds(textScanLoopDelay);
            }
        }
        #endregion
    }
}
