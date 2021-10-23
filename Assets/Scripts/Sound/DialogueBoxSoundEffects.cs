using Frankie.Speech.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Sound
{
    public class DialogueBoxSoundEffects : SoundEffects
    {
        // Tunables
        [SerializeField] DialogueBox dialogueBox = null;
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
            dialogueBox.dialogueBoxModified += HandleDialogueBoxUpdate;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            dialogueBox.dialogueBoxModified -= HandleDialogueBoxUpdate;
        }

        private void HandleDialogueBoxUpdate(DialogueBoxModifiedType dialogueBoxModifiedType, bool enable)
        {
            if (dialogueBoxModifiedType == DialogueBoxModifiedType.writingStateChanged)
            {
                ConfigureTextScanAudio(enable);
            }
            else if (dialogueBoxModifiedType == DialogueBoxModifiedType.itemSelected)
            {
                PlayClip(chooseAudioClip);
            }
            else if (dialogueBoxModifiedType == DialogueBoxModifiedType.clientEnter)
            {
                PlayClip(enterClip);
            }
            else if (dialogueBoxModifiedType == DialogueBoxModifiedType.clientExit)
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
