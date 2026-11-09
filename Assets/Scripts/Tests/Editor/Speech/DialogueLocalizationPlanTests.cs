using System.Collections.Generic;
using NUnit.Framework;
using Frankie.Speech.Editor;

namespace Frankie.Tests.Editor
{
    public class DialogueLocalizationPlanTests
    {
        // Covers DialogueLocalizationPlan: three-way graph / table / last-synced compare (w/out tables)
        // Not covered: applying the plan (DialogueGraphLocalizationSync writes the Speech table)

        // Const Tunables
        private const string _baseID = "line.Text";
        private const string _key = "Dialogue.0.0.Text";

        #region Tests
        [Test]
        public void Plan_GraphMatchesTable_SettlesBaseWithoutWrites()
        {
            DialogueLocalizationPlan plan = MakePlan("Hello", table: "Hello", lastSynced: null);

            Assert.IsFalse(plan.hasTableWrites || plan.hasGraphWrites);
            Assert.AreEqual("Hello", plan.settledBases[_baseID]);
        }

        [Test]
        public void Plan_NoTableEntry_Pushes()
        {
            DialogueLocalizationPlan plan = MakePlan("Hello", table: null, lastSynced: null);

            Assert.AreEqual("Hello", plan.pushes[_key]);
        }

        [Test]
        public void Plan_NoLastSynced_TableWins()
        {
            DialogueLocalizationPlan plan = MakePlan("Graph", table: "Table", lastSynced: null);

            Assert.AreEqual("Table", plan.pulls[_baseID]);
            Assert.IsFalse(plan.hasTableWrites);
        }

        [Test]
        public void Plan_GraphEdited_Pushes()
        {
            DialogueLocalizationPlan plan = MakePlan("New", table: "Old", lastSynced: "Old");

            Assert.AreEqual("New", plan.pushes[_key]);
            Assert.IsEmpty(plan.conflicts);
        }

        [Test]
        public void Plan_TableEdited_Pulls()
        {
            DialogueLocalizationPlan plan = MakePlan("Old", table: "New", lastSynced: "Old");

            Assert.AreEqual("New", plan.pulls[_baseID]);
            Assert.IsFalse(plan.hasTableWrites);
        }

        [Test]
        public void Plan_BothEdited_GraphWinsAndReportsConflict()
        {
            DialogueLocalizationPlan plan = MakePlan("Graph", table: "Table", lastSynced: "Old");

            Assert.AreEqual("Graph", plan.pushes[_key]);
            CollectionAssert.AreEqual(new[] { _key }, plan.conflicts);
        }

        [Test]
        public void Plan_EntryNoLineClaims_IsRemoved()
        {
            var tableEntries = new Dictionary<string, string> { { _key, "Hello" }, { "Dialogue.3.1.Text", "Orphan" }, { "Dialogue.3.1.Speaker", "" } };

            DialogueLocalizationPlan plan = new DialogueLocalizationPlan(new[] { new DialogueLocalizationField(_baseID, _key, "Hello") }, tableEntries, new Dictionary<string, string>());

            CollectionAssert.AreEqual(new[] { "Dialogue.3.1.Speaker", "Dialogue.3.1.Text" }, plan.removals);
        }

        [Test]
        public void Plan_NullGraphValue_TreatedAsEmpty()
        {
            DialogueLocalizationPlan plan = MakePlan(null, table: "", lastSynced: null);

            Assert.IsFalse(plan.hasTableWrites || plan.hasGraphWrites);
        }
        #endregion

        #region Helpers
        private static DialogueLocalizationPlan MakePlan(string graph, string table, string lastSynced)
        {
            var tableEntries = new Dictionary<string, string>();
            if (table != null) { tableEntries[_key] = table; }
            var bases = new Dictionary<string, string>();
            if (lastSynced != null) { bases[_baseID] = lastSynced; }
            return new DialogueLocalizationPlan(new[] { new DialogueLocalizationField(_baseID, _key, graph) }, tableEntries, bases);
        }
        #endregion
    }
}
