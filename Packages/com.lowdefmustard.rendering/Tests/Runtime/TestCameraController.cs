using System.Collections.Generic;
using UnityEngine;

namespace LowDefMustard.Rendering.Tests
{
    internal sealed class TestCameraController : CameraControllerBase
    {
        public readonly Queue<GameObject> queuedTargets = new();
        public readonly List<bool> subscriptions = new();
        public GameObject fallbackTarget;

        protected override GameObject FindDefaultFollowTarget() => queuedTargets.Count > 0 ? queuedTargets.Dequeue() : fallbackTarget;
        protected override void SubscribeToDefaultFollowTarget(bool subscribe) => subscriptions.Add(subscribe);

        public void AnnounceDefaultFollowTarget(GameObject target) => SetDefaultFollowTarget(target);
    }
}
