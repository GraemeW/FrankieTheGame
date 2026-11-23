#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Utils.Tests;
using Frankie.Quests;

namespace Frankie.Tests
{
    public class QuestListBuildCopyTests
    {
        // Tested:
        //  - QuestList.GetQuestStatus / HasQuest - a build copy of a held quest finds its status (by GUID)
        //  - QuestList.TryAddQuest - adding a build copy of a held quest returns the existing status, no second entry
        // Not Tested:
        //  - QuestList.CompleteObjective - resolves the quest through the Quest addressables cache (by ID already)
        // Note:  Quests are in memory (QuestTestBuilders);  play mode for QuestList.Awake

        // State
        private readonly TestObjectTracker tracker = new();
        private QuestList questList;
        private Quest quest;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            questList = tracker.Track(new GameObject("QuestListBuildCopyTests_QuestList")).AddComponent<QuestList>();
            quest = tracker.CreateQuest("QuestListBuildCopyTests_Quest", 1);
        }

        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
        }
        #endregion

        #region Tests
        [Test]
        public void GetQuestStatus_HeldQuestBuildCopy_FindsStatus()
        {
            QuestStatus questStatus = questList.TryAddQuest(quest);
            Quest questBuildCopy = tracker.CreateBuildCopy(quest);

            Assert.IsTrue(questList.HasQuest(questBuildCopy));
            Assert.AreSame(questStatus, questList.GetQuestStatus(questBuildCopy));
        }

        [Test]
        public void TryAddQuest_HeldQuestBuildCopy_ReturnsExistingStatus()
        {
            QuestStatus questStatus = questList.TryAddQuest(quest);

            Assert.AreSame(questStatus, questList.TryAddQuest(tracker.CreateBuildCopy(quest)));
            Assert.AreEqual(1, questList.GetActiveQuests().Count());
        }
        #endregion
    }
}
#endif
