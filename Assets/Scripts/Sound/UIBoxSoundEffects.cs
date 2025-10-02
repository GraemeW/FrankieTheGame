using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils.UI;

namespace Frankie.Sound
{
    public class UIBoxSoundEffects : SoundEffects
    {
        // Tunables
        [SerializeField] UIBox uiBox = null;
        [SerializeField] AudioClip textScanAudioClip = null;
        [SerializeField] AudioClip chooseAudioClip = null;
        [SerializeField] AudioClip enterClip = null;
        [SerializeField] AudioClip exitClip = null;
        [SerializeField] float textScanLoopDelay = 0.1f;

        // State
        bool isTextScanActive = false;

        private void Start()
        {
            audioSource.clip = textScanAudioClip;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            uiBox.uiBoxModified += HandleDialogueBoxUpdate;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            uiBox.uiBoxModified -= HandleDialogueBoxUpdate;
        }

        private void HandleDialogueBoxUpdate(UIBoxModifiedType uiBoxModifiedType, bool enable)
        {
            if (uiBoxModifiedType == UIBoxModifiedType.writingStateChanged)
            {
                ConfigureTextScanAudio(enable);
            }
            else if (uiBoxModifiedType == UIBoxModifiedType.itemSelected)
            {
                PlayClip(chooseAudioClip);
            }
            else if (uiBoxModifiedType == UIBoxModifiedType.clientEnter)
            {
                PlayClip(enterClip);
            }
            else if (uiBoxModifiedType == UIBoxModifiedType.clientExit)
            {
                PlayClipAfterDestroy(exitClip);
            }
        }

        private void ConfigureTextScanAudio(bool enable)
        {
            if (enable)
            {
                InitializeVolume();
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
    }
}
