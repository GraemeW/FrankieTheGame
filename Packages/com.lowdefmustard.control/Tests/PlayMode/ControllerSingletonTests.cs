using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.Control.Tests.PlayMode
{
    public class ControllerSingletonTests
    {
        private class TestController : BaseController
        {
            protected override void OnNoReceiversIdentified() { }
            public bool CallVerifyUnique() => VerifyUnique();
        }

        [UnityTest]
        public IEnumerator VerifyUnique_SingleInstance_ReturnsTrueAndSurvives()
        {
            var go = new GameObject("ControllerSingletonTests_Solo");
            var controller = go.AddComponent<TestController>();

            bool result = controller.CallVerifyUnique();
            yield return null;

            Assert.That(result, Is.True);
            Assert.That(go != null, Is.True);

            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator VerifyUnique_SecondInstance_ReturnsFalseAndDestroysGameObjectByNextFrame()
        {
            var go1 = new GameObject("ControllerSingletonTests_First");
            var go2 = new GameObject("ControllerSingletonTests_Second");
            var controller1 = go1.AddComponent<TestController>();
            var controller2 = go2.AddComponent<TestController>();

            bool result = controller2.CallVerifyUnique();

            Assert.That(result, Is.False);
            yield return null;  // Destroy() defers destruction 'til end of the frame
            Assert.That(go2 == null, Is.True, "GameObject should be destroyed by the next frame");
            Assert.That(go1 != null, Is.True, "The other instance should be untouched");

            Object.Destroy(go1);
            yield return null;
        }

        [UnityTest]
        public IEnumerator VerifyUnique_AfterDuplicateDestroyed_RemainingInstanceReportsUnique()
        {
            var go1 = new GameObject("ControllerSingletonTests_First");
            var go2 = new GameObject("ControllerSingletonTests_Second");
            var controller1 = go1.AddComponent<TestController>();
            var controller2 = go2.AddComponent<TestController>();

            controller2.CallVerifyUnique(); 
            yield return null; // Destroy() defers destruction 'til end of the frame

            bool result = controller1.CallVerifyUnique();

            Assert.That(result, Is.True);
            Assert.That(go1 != null, Is.True);

            Object.Destroy(go1);
            yield return null;
        }
    }
}
