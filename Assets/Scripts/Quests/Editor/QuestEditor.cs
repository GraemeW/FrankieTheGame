using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace Frankie.Quests.UIEditor
{
    [CustomEditor(typeof(Quest))]
    public class QuestEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Quest quest = (Quest)target;
            if (GUILayout.Button("Generate Objectives"))
            {
                quest.GenerateObjectiveFromNames();
            }
        }
    }
}

