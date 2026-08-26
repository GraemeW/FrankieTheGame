using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class StandardCanvasZoomManipulatorTests
    {
        private EditorWindow window;

        [TearDown]
        public void TearDown()
        {
            if (window != null) { window.Close(); }
        }

        [Test]
        public void AddManipulator_SetsTarget_NoPanelRequired()
        {
            var element = new VisualElement();
            var zoomTarget = new VisualElement();
            var manipulator = new StandardCanvasZoomManipulator(zoomTarget);

            element.AddManipulator(manipulator);

            Assert.AreSame(element, manipulator.target);
        }

        [Test]
        public void Constructor_SetsZoomTargetTransformOriginToZero()
        {
            var zoomTarget = new VisualElement();

            _ = new StandardCanvasZoomManipulator(zoomTarget);

            Assert.AreEqual(new TransformOrigin(0, 0), zoomTarget.style.transformOrigin.value);
        }

        private (StandardCanvasZoomManipulator manipulator, VisualElement zoomTarget) SetUpAttachedManipulator()
        {
            window = HeadlessEditorWindowTestHelper.CreateOffscreenWindow();
            var zoomTarget = new VisualElement();
            window.rootVisualElement.Add(zoomTarget);
            var manipulator = new StandardCanvasZoomManipulator(zoomTarget);
            window.rootVisualElement.AddManipulator(manipulator);
            return (manipulator, zoomTarget);
        }

        private static void SendWheel(VisualElement target, float deltaY)
        {
            using var wheelEvent = WheelEvent.GetPooled(new Event { type = EventType.ScrollWheel, delta = new Vector3(0, deltaY, 0) });
            wheelEvent.target = target;
            target.SendEvent(wheelEvent);
        }

        [UnityTest]
        public IEnumerator OnWheel_ScrollingUp_IncreasesZoomFactorAndFiresEvent()
        {
            var (manipulator, _) = SetUpAttachedManipulator();
            yield return null;

            float? receivedZoom = null;
            manipulator.zoomChanged += zoom => receivedZoom = zoom;

            SendWheel(window.rootVisualElement, deltaY: -1);
            yield return null;

            Assert.Greater(manipulator.zoomFactor, 1f, $"Expected zoom to increase from 1, got {manipulator.zoomFactor}");
            Assert.IsTrue(receivedZoom.HasValue, "Expected zoomChanged to fire");
        }

        [UnityTest]
        public IEnumerator OnWheel_ScrollingDown_DecreasesZoomFactor()
        {
            var (manipulator, _) = SetUpAttachedManipulator();
            yield return null;

            SendWheel(window.rootVisualElement, deltaY: 1);
            yield return null;

            Assert.Less(manipulator.zoomFactor, 1f, $"Expected zoom to decrease from 1, got {manipulator.zoomFactor}");
        }

        [UnityTest]
        public IEnumerator OnWheel_RepeatedScrollingUp_ClampsAtMaxZoom()
        {
            var (manipulator, _) = SetUpAttachedManipulator();
            yield return null;

            // Enough notches to comfortably exceed the 2x max if unclamped
            for (int i = 0; i < 50; i++)
            {
                SendWheel(window.rootVisualElement, deltaY: -1);
            }
            yield return null;

            Assert.AreEqual(2f, manipulator.zoomFactor, 0.001f);
        }

        [UnityTest]
        public IEnumerator OnWheel_RepeatedScrollingDown_ClampsAtMinZoom()
        {
            var (manipulator, _) = SetUpAttachedManipulator();
            yield return null;

            for (int i = 0; i < 50; i++)
            {
                SendWheel(window.rootVisualElement, deltaY: 1);
            }
            yield return null;

            Assert.AreEqual(0.25f, manipulator.zoomFactor, 0.001f);
        }

        [UnityTest]
        public IEnumerator OnWheel_AtMaxZoom_FurtherScrollUp_DoesNotFireZoomChangedAgain()
        {
            // Exercises the Mathf.Approximately early-return
            // Once clamped, a further nudge in the same direction should be a no-op, not a redundant event
            var (manipulator, _) = SetUpAttachedManipulator();
            yield return null;

            for (int i = 0; i < 50; i++)
            {
                SendWheel(window.rootVisualElement, deltaY: -1);
            }
            yield return null;

            int fireCountAtMax = 0;
            manipulator.zoomChanged += _ => fireCountAtMax++;
            SendWheel(window.rootVisualElement, deltaY: -1);
            yield return null;

            Assert.AreEqual(0, fireCountAtMax);
        }
    }
}
