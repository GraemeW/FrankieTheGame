using NUnit.Framework;
using LowDefMustard.Utils.Tests;
using Frankie.Quests;

namespace Frankie.Tests.Editor
{
    public class QuestBuildCopyTests
    {
        // Tested:
        //  - Quest - build copy keeps the GUID;  HasObjective / GetObjectiveFromID match a build copy of an objective
        //  - QuestStatus.GetStatusForObjective - completed via one copy, read via the other
        //  - QuestStatus.SetObjective - completing via both copies counts once;  un-completing via the other copy removes it
        // Not Tested:
        //  - QuestList quest matching - needs QuestList.Awake, so it's in QuestListBuildCopyTests (play mode)
        //  - QuestStatus(SerializableQuestStatus) restore - resolves through the Quest addressables cache, so by ID already
        // Note:  Quests + objectives are in memory (QuestTestBuilders)

        // State
        private readonly TestObjectTracker tracker = new();
        private Quest quest;
        private QuestObjective objective;
        private QuestObjective objectiveBuildCopy;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            quest = tracker.CreateQuest("QuestBuildCopyTests_Quest", 2);
            objective = quest.GetObjectives()[0];
            objectiveBuildCopy = tracker.CreateBuildCopy(objective);
        }

        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
        }
        #endregion

        #region Tests
        [Test]
        public void Quest_BuildCopy_KeepsGUID()
        {
            Quest questBuildCopy = tracker.CreateBuildCopy(quest);

            Assert.IsNotEmpty(quest.GetGUID());
            Assert.AreEqual(quest.GetGUID(), questBuildCopy.GetGUID());
        }

        [Test]
        public void Quest_ObjectiveBuildCopy_IsMatched()
        {
            Assert.AreNotSame(objective, objectiveBuildCopy);
            Assert.IsTrue(quest.HasObjective(objectiveBuildCopy));
            Assert.AreSame(objective, quest.GetObjectiveFromID(objectiveBuildCopy.GetObjectiveID()));
        }

        [Test]
        public void GetStatusForObjective_CompletedViaOtherCopy_IsComplete()
        {
            var questStatus = new QuestStatus(quest);

            questStatus.SetObjective(objective, true);

            Assert.IsTrue(questStatus.GetStatusForObjective(objectiveBuildCopy));
        }

        [Test]
        public void SetObjective_CompletedViaBothCopies_CountsOnce()
        {
            var questStatus = new QuestStatus(quest);

            questStatus.SetObjective(objective, true);
            questStatus.SetObjective(objectiveBuildCopy, true);

            Assert.AreEqual(1, questStatus.GetCompletedObjectiveCount());
        }

        [Test]
        public void SetObjective_UncompletedViaOtherCopy_IsRemoved()
        {
            var questStatus = new QuestStatus(quest);
            questStatus.SetObjective(objective, true);

            questStatus.SetObjective(objectiveBuildCopy, false);

            Assert.AreEqual(0, questStatus.GetCompletedObjectiveCount());
            Assert.IsFalse(questStatus.GetStatusForObjective(objective));
        }
        #endregion
    }
}
