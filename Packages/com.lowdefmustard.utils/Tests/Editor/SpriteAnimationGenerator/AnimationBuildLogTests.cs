using NUnit.Framework;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class AnimationBuildLogTests
    {
        [Test]
        public void Constructor_WithNullLabel_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new AnimationBuildLog(null));
        }

        [Test]
        public void Constructor_WithLabel_StartsEmpty()
        {
            var label = new Label();

            _ = new AnimationBuildLog(label);

            Assert.AreEqual(string.Empty, label.text);
        }

        [Test]
        public void AppendLine_PublishesTextToLabel()
        {
            var label = new Label();
            var log = new AnimationBuildLog(label);

            log.AppendLine("hello");

            StringAssert.Contains("hello", label.text);
        }

        [Test]
        public void SummarizeGeneration_PrependsCreatedAndSkippedCounts()
        {
            var label = new Label();
            var log = new AnimationBuildLog(label);
            log.createdCount = 3;
            log.SkipNoSprite("clip1"); // increments the private skipped count to 1
            log.SkipAlreadyExists("clip2"); // increments it to 2

            log.SummarizeGeneration();

            StringAssert.StartsWith(string.Format(AnimationBuildLog.summarizeMessage, 3, 2), label.text);
        }

        [Test]
        public void SkipNoSprite_AppendsExpectedMessage()
        {
            var label = new Label();
            var log = new AnimationBuildLog(label);

            log.SkipNoSprite("MyClip");

            StringAssert.Contains(string.Format(AnimationBuildLog.skipNoSpriteMessage, "MyClip"), label.text);
        }

        [Test]
        public void SkipAlreadyExists_AppendsExpectedMessage()
        {
            var label = new Label();
            var log = new AnimationBuildLog(label);

            log.SkipAlreadyExists("MyClip");

            StringAssert.Contains(string.Format(AnimationBuildLog.skipAlreadyExistsMessage, "MyClip"), label.text);
        }

        [Test]
        public void AnnotatePassthroughActions_EmptySet_AddsNothing()
        {
            var label = new Label();
            var log = new AnimationBuildLog(label);

            log.AnnotatePassthroughActions(new System.Collections.Generic.HashSet<string>());

            Assert.AreEqual(string.Empty, label.text);
        }

        [Test]
        public void AnnotatePassthroughActions_NonEmptySet_ListsActionsSorted()
        {
            var label = new Label();
            var log = new AnimationBuildLog(label);

            log.AnnotatePassthroughActions(new System.Collections.Generic.HashSet<string> { "Zeta", "Alpha" });

            int alphaIndex = label.text.IndexOf("Alpha", System.StringComparison.Ordinal);
            int zetaIndex = label.text.IndexOf("Zeta", System.StringComparison.Ordinal);
            Assert.Greater(alphaIndex, -1);
            Assert.Greater(zetaIndex, -1);
            Assert.Less(alphaIndex, zetaIndex); // Alpha should be listed before Zeta
        }

        [Test]
        public void AnnotateAmbiguousActions_EmptyList_AddsNothing()
        {
            var label = new Label();
            var log = new AnimationBuildLog(label);

            log.AnnotateAmbiguousActions(new System.Collections.Generic.List<string>());

            Assert.AreEqual(string.Empty, label.text);
        }

        [Test]
        public void AnnotateAmbiguousActions_NonEmptyList_ListsActionsSorted()
        {
            var label = new Label();
            var log = new AnimationBuildLog(label);

            log.AnnotateAmbiguousActions(new System.Collections.Generic.List<string> { "Zeta", "Alpha" });

            int alphaIndex = label.text.IndexOf("Alpha", System.StringComparison.Ordinal);
            int zetaIndex = label.text.IndexOf("Zeta", System.StringComparison.Ordinal);
            Assert.Less(alphaIndex, zetaIndex);
        }
    }
}
