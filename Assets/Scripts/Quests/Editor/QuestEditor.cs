using Frankie.Core.GameStateModifiers;
using UnityEngine;
using UnityEditor;

namespace Frankie.Quests.Editor
{
    [CustomEditor(typeof(Quest))]
    public class QuestEditor : GameStateModifierEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MakeQuestHeader("Quest Functionality");
            
            Quest quest = (Quest)target;
            if (GUILayout.Button("Generate Objectives (Save to Take Effect)"))
            {
                quest.GenerateObjectiveFromNames();
            }
        }

        private void MakeQuestHeader(string headerTitle)
        {
            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(headerTitle, headerStyle);

            }
            EditorGUILayout.Space(4);
        }
    }
}
