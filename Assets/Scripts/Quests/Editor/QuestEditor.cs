using UnityEngine;
using UnityEditor;

namespace Frankie.Quests.UIEditor
{
    [CustomEditor(typeof(Quest))]
    public class QuestEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Quest quest = (Quest)target;
            if (GUILayout.Button("Generate Objectives (Save to Take Effect)"))
            {
                quest.GenerateObjectiveFromNames();
            }
        }
    }
}
