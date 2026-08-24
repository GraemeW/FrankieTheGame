using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.Utils.Tests.PlayMode
{
    public class EditorStateCheckPlayModeTests
    {
        [UnityTest]
        public IEnumerator IsStandardEditorState_DuringPlayMode_ReturnsFalse()
        {
            // Application.isPlaying and EditorApplication.isPlaying are both true throughout a Play Mode test
            // i.e. they fire together, so this test can't isolate one from the other, only confirm the combined "in Play Mode" case
            var gameObject = new GameObject("Temp");

            yield return null;

            Assert.IsFalse(EditorStateCheck.IsStandardEditorState(gameObject));

            Object.Destroy(gameObject);
        }
    }
}
