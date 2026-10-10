using System;
using System.Collections;
using UnityEngine;
using LowDefMustard.UIBox;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class NamingStage : MonoBehaviour
    {
        // Tunables
        [SerializeField] private float walkSeconds = 1.5f;
        [SerializeField] private float activeWalkAnimationRate = 1f;
        [SerializeField] private float plantedWalkAnimationRate = 0.25f;

        // State
        private AnimatorSpriteSource characterSource;
        private WalkingSpriteHandle characterHandle;
        private Coroutine activeRoutine;

        // Cached References
        private UIToolkitMenuView menuView;

        #region UnityMethods
        private void Awake()
        {
            menuView = GetComponent<UIToolkitMenuView>();
        }

        private void OnDisable()
        {
            StopActiveRoutine();
            RemoveCharacter();
        }
        #endregion

        #region PublicMethods
        public void ShowCharacter(GameObject characterPrefab) // Null:  the stage is left empty
        {
            RestartRoutine(SwapCharacter(characterPrefab));
        }

        public void ClearCharacter(bool walkOff, Action onCleared)
        {
            if (walkOff) { RestartRoutine(WalkOffThen(onCleared)); return; }

            StopActiveRoutine();
            RemoveCharacter();
            onCleared?.Invoke();
        }
        #endregion

        #region PrivateMethods
        private void RestartRoutine(IEnumerator routine)
        {
            StopActiveRoutine();
            activeRoutine = StartCoroutine(routine);
        }

        private void StopActiveRoutine()
        {
            if (activeRoutine != null) { StopCoroutine(activeRoutine); }
            activeRoutine = null;
        }

        private IEnumerator SwapCharacter(GameObject characterPrefab)
        {
            yield return WalkOff();
            if (!AnimatorSpriteSource.TryCreate(characterPrefab, transform, out characterSource)) { yield break; }

            characterHandle = new WalkingSpriteHandle(menuView, characterSource.spriteModel, typeof(NamingStageElement), 1f);
            menuView.AddEntry(characterHandle);
            yield return WalkTo(0f);
            characterSource.PoseWalk(Vector2.down, plantedWalkAnimationRate);
        }

        private IEnumerator WalkTo(float toOffStageFraction)
        {
            float fromOffStageFraction = characterHandle.offStageFraction;
            float walkDuration = walkSeconds * Mathf.Abs(toOffStageFraction - fromOffStageFraction);
            if (walkDuration <= 0f) { yield break; }

            // Off-stage is to the left -> so walking on faces right
            characterSource.PoseWalk(toOffStageFraction < fromOffStageFraction ? Vector2.right : Vector2.left, activeWalkAnimationRate);

            for (float walkTime = 0f; walkTime < walkDuration; walkTime += Time.deltaTime)
            {
                characterHandle.SetOffStageFraction(Mathf.Lerp(fromOffStageFraction, toOffStageFraction, walkTime / walkDuration));
                yield return null;
            }
            characterHandle.SetOffStageFraction(toOffStageFraction);
        }
        
        private IEnumerator WalkOff()
        {
            if (characterSource != null && characterHandle != null) { yield return WalkTo(1f); }
            RemoveCharacter();
        }
        
        private IEnumerator WalkOffThen(Action onComplete)
        {
            yield return WalkOff();
            onComplete?.Invoke();
        }
        
        private void RemoveCharacter()
        {
            characterHandle?.Remove();
            characterHandle = null;
            if (characterSource != null) { Destroy(characterSource.gameObject); }
            characterSource = null;
        }
        #endregion
    }
}
