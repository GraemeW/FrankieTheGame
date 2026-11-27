using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Utils.Tests;
using Frankie.Speech;

namespace Frankie.Tests.Editor
{
    public class DialogueVariableFormatterTests
    {
        // Tested:
        //  - DialogueVariableFormatter.Format - ordering, repeats, more / fewer placeholders than variables, null / destroyed / non-implementer / null-returning variables, other brace text, context pass-through
        //  - DialogueVariableFormatter.GetPlaceholderIndices / GetContiguousCount - gaps, repeats, no placeholders
        //  - DialogueVariableContext.TryGetParty / TryGetConversant - set, default and destroyed
        //  - DialogueNode.GetDialogueVariables - set via Initialize
        // Not Tested:
        //  - DialogueNode.GetText(context) end to end - needs a populated Speech table entry (formatting covered here)

        // State
        private readonly TestObjectTracker tracker = new();
        private TestDialogueVariable alpha;
        private TestDialogueVariable beta;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            alpha = tracker.Create<TestDialogueVariable>("Alpha").Configure("Alpha");
            beta = tracker.Create<TestDialogueVariable>("Beta").Configure("Beta");
        }

        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private static string Format(string text, params ScriptableObject[] variables) => DialogueVariableFormatter.Format(text, variables, default);
        #endregion

        #region Tests
        [Test]
        public void Format_NoPlaceholders_ReturnsTextUnchanged()
        {
            Assert.AreEqual("Hello there", Format("Hello there", alpha));
            Assert.AreEqual("", Format("", alpha));
        }

        [Test]
        public void Format_NullInput_ReturnsEmptyString()
        {
            Assert.AreEqual(Format(null, alpha), "");
        }

        [Test]
        public void Format_Placeholders_ResolveByIndexInAnyOrder()
        {
            Assert.AreEqual("Beta gave Alpha a hat", Format("{1} gave {0} a hat", alpha, beta));
        }

        [Test]
        public void Format_RepeatedPlaceholder_ResolvesVariableOncePerCall()
        {
            Assert.AreEqual("Alpha, Alpha! Beta, Alpha", Format("{0}, {0}! {1}, {0}", alpha, beta));
            Assert.AreEqual(1, alpha.resolveCount);
            Assert.AreEqual(1, beta.resolveCount);

            Format("{0} {0}", alpha);
            Assert.AreEqual(2, alpha.resolveCount, "Cache is per call, so the next call resolves afresh");
        }

        [Test]
        public void Format_MorePlaceholdersThanVariables_StripsExtras()
        {
            Assert.AreEqual("Alpha and  and ", Format("{0} and {1} and {5}", alpha));
        }

        [Test]
        public void Format_MoreVariablesThanPlaceholders_IgnoresExtras()
        {
            Assert.AreEqual("Hi Alpha", Format("Hi {0}", alpha, beta));
            Assert.AreEqual(0, beta.resolveCount);
        }

        [Test]
        public void Format_NullVariableList_StripsPlaceholders()
        {
            Assert.AreEqual("Hi ", DialogueVariableFormatter.Format("Hi {0}", null, default));
        }

        [Test]
        public void Format_NullSlot_ShowsBlank()
        {
            Assert.AreEqual(" meets Beta", Format("{0} meets {1}", null, beta));
        }

        [Test]
        public void Format_DestroyedVariable_ShowsBlank()
        {
            var destroyed = ScriptableObject.CreateInstance<TestDialogueVariable>().Configure("Gone");
            Object.DestroyImmediate(destroyed);
            Assert.AreEqual("Hi ", Format("Hi {0}", destroyed));
        }

        [Test]
        public void Format_NonImplementer_ShowsBlank()
        {
            var predicate = tracker.Create<TestPredicate>("NotAVariable");
            Assert.AreEqual("Hi ", Format("Hi {0}", predicate));
        }

        [Test]
        public void Format_VariableReturnsNull_ShowsBlank()
        {
            alpha.Configure(null);
            Assert.AreEqual("Hi ", Format("Hi {0}", alpha));
        }

        [Test]
        public void Format_OtherBraceText_LeftAsIs()
        {
            Assert.AreEqual("{name} { } {-1} {0x} Alpha", Format("{name} { } {-1} {0x} {0}", alpha));
        }

        [Test]
        public void Format_PassesContextToVariable()
        {
            var conversant = tracker.CreateComponent<AIConversant>("Conversant");
            DialogueVariableFormatter.Format("{0}", new ScriptableObject[] { alpha }, new DialogueVariableContext(null, conversant));
            Assert.IsTrue(alpha.lastContext.TryGetConversant(out AIConversant passedConversant));
            Assert.AreSame(conversant, passedConversant);
            Assert.IsFalse(alpha.lastContext.TryGetParty(out _));
        }

        [Test]
        public void Context_DefaultOrDestroyed_TryGetsFail()
        {
            Assert.IsFalse(default(DialogueVariableContext).TryGetParty(out _));
            Assert.IsFalse(default(DialogueVariableContext).TryGetConversant(out _));

            var conversantObject = new GameObject("Conversant");
            var context = new DialogueVariableContext(null, conversantObject.AddComponent<AIConversant>());
            Object.DestroyImmediate(conversantObject);
            Assert.IsFalse(context.TryGetConversant(out _));
        }

        [Test]
        public void GetPlaceholderIndices_ReturnsDistinctSortedIndices()
        {
            CollectionAssert.AreEqual(new[] { 0, 2, 3 }, DialogueVariableFormatter.GetPlaceholderIndices("{3} {0} {2} {0} {x}"));
            CollectionAssert.IsEmpty(DialogueVariableFormatter.GetPlaceholderIndices("No placeholders"));
            CollectionAssert.IsEmpty(DialogueVariableFormatter.GetPlaceholderIndices(null));
        }

        [TestCase("No placeholders", 0)]
        [TestCase("{1} only", 0)]
        [TestCase("{0} then {2}", 1)]
        [TestCase("{1} {0} {2}", 3)]
        [TestCase("{0} {0}", 1)]
        public void GetContiguousCount_CountsFromZeroToFirstGap(string text, int expected)
        {
            Assert.AreEqual(expected, DialogueVariableFormatter.GetContiguousCount(text));
        }

        [Test]
        public void DialogueNode_Initialize_KeepsVariablesInOrder()
        {
            var dialogueNode = tracker.Create<DialogueNode>("Line");
            dialogueNode.Initialize("Line", SpeakerType.AISpeaker, null, 0, 0, new UnityEngine.Localization.LocalizedString(), new UnityEngine.Localization.LocalizedString(), new string[0], new ScriptableObject[] { alpha, null, beta });
            CollectionAssert.AreEqual(new ScriptableObject[] { alpha, null, beta }, dialogueNode.GetDialogueVariables());
        }
        #endregion
    }
}
