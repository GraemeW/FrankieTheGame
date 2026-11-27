#if UNITY_EDITOR
using System;
using System.Globalization;
using System.Linq;
using LowDefMustard.Utils.Tests;
using Frankie.Quests;

namespace Frankie.Tests
{
    internal static class QuestTestBuilders
    {
        // Note:  Objects are owned by the tracker, so destroy via its DestroyAll in TearDown
        // In memory only ~ QuestObjective.Setup is skipped since it writes localization table entries

        #region PublicMethods
        public static Quest CreateQuest(this TestObjectTracker tracker, string questName, int objectiveCount)
        {
            var quest = tracker.Create<Quest>(questName);
            QuestObjective[] questObjectives = Enumerable.Range(0, objectiveCount).Select(index => tracker.CreateObjective($"{questName}_Objective{index}")).ToArray();
            SerializedFieldInjector.SetArray(quest, Quest.questObjectivesField, questObjectives); // Serializing also assigns the quest's GUID
            return quest;
        }
        #endregion

        #region PrivateMethods
        private static QuestObjective CreateObjective(this TestObjectTracker tracker, string objectiveName)
        {
            var questObjective = tracker.Create<QuestObjective>(objectiveName);
            questObjective.objectiveID = Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);
            return questObjective;
        }
        #endregion
    }
}
#endif
