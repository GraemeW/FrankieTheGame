using System.Collections.Generic;
using Frankie.Control;
using UnityEngine;
using LowDefMustard.Control;

namespace Frankie.Utils.UI
{
    public sealed class AnimatorSpriteSource : MonoBehaviour
    {
        // Background:
        //  - already have game object prefabs w/ animators, and we want UI sprites to mirror them
        //  - so create a hidden puppet (derived from the prefab), run the animator but never draw it, and copy the sprite data onto UI sprite model
        
        // Const
        private const string _puppetSuffix = " (Sprite Source)";
        private const float _movingSpeedParameter = 1f; // Note:  Controller only reads this as a gate (> 0.1 -> Move, else Idle)

        // State
        public SpriteModel spriteModel { get; } = new();
        private Animator animator;
        private SpriteRenderer spriteRenderer;

        #region StaticMethods
        public static bool TryCreate(GameObject sourcePrefab, Transform parent, out AnimatorSpriteSource animatorSpriteSource)
        {
            // Note:  We only need to read the Animator off the prefab asset - the prefab itself is never instantiated
            
            animatorSpriteSource = null;
            if (sourcePrefab == null || !sourcePrefab.TryGetComponent(out CharacterMoveLink characterMoveLink)) { return false; }

            Animator sourceAnimator = characterMoveLink.GetAnimator();
            if (sourceAnimator == null || sourceAnimator.runtimeAnimatorController == null) { return false; }
            SpriteRenderer sourceSpriteRenderer = characterMoveLink.GetSpriteRenderer(); 
            if (sourceSpriteRenderer == null) { return false; }

            var puppetObject = new GameObject($"{sourcePrefab.name}{_puppetSuffix}");
            puppetObject.transform.SetParent(parent, false);
            animatorSpriteSource = puppetObject.AddComponent<AnimatorSpriteSource>();
            animatorSpriteSource.Initialize(sourceAnimator, sourceSpriteRenderer);
            return true;
        }
        #endregion

        #region UnityMethods
        private void LateUpdate()
        {
            if (spriteRenderer == null) { return; }
            spriteModel.sprite = spriteRenderer.sprite; // No-op unless the Animator swapped the frame
        }
        #endregion

        #region PublicMethods
        public void PoseWalk(Vector2 lookDirection, float animationSpeed)
        {
            if (animator == null) { return; }
            Mover.SetAnimatorXLook(animator, lookDirection.x);
            Mover.SetAnimatorYLook(animator, lookDirection.y);
            Mover.SetAnimatorSpeed(animator, _movingSpeedParameter);
            animator.speed = animationSpeed;
        }
        #endregion
        
        #region PrivateMethods
        private void Initialize(Animator sourceAnimator, SpriteRenderer sourceSpriteRenderer)
        {
            // Mimic the prefab's path from Animator to SpriteRenderer to ensure animation binding holds
            Transform rendererTransform = transform;
            foreach (string pathName in GetPathNames(sourceAnimator.transform, sourceSpriteRenderer.transform))
            {
                var pathObject = new GameObject(pathName);
                pathObject.transform.SetParent(rendererTransform, false);
                rendererTransform = pathObject.transform;
            }

            spriteRenderer = rendererTransform.gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sourceSpriteRenderer.sprite;
            spriteRenderer.enabled = false;
            spriteModel.sprite = spriteRenderer.sprite;
            
            animator = gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = sourceAnimator.runtimeAnimatorController;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private static List<string> GetPathNames(Transform root, Transform target)
        {
            var pathNames = new List<string>();
            for (Transform current = target; current != null && current != root; current = current.parent)
            {
                pathNames.Insert(0, current.name);
            }
            return pathNames;
        }
        #endregion
    }
}
