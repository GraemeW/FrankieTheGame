namespace Frankie.ZoneManagement.Editor
{
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(MoveMesh))]
    public class EnclosedRegionFinderEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var defaultInspector = new VisualElement();
            InspectorElement.FillDefaultInspector(defaultInspector, serializedObject, this);
            root.Add(defaultInspector);

            root.Add(new VisualElement { style = { height = 8 } });

            VisualElement divider = MakeDivider();
            root.Add(divider);

            Button runButton = MakeButton("Run Detection");
            runButton.RegisterCallback<ClickEvent>(_ => RunDetection());
            root.Add(runButton);

            Button clearButton = MakeButton("Clear Data");
            clearButton.RegisterCallback<ClickEvent>(_ => ClearData());
            root.Add(clearButton);
            return root;
        }

        private void RunDetection()
        {
            var moveMesh = (MoveMesh)target;
            Undo.RecordObject(moveMesh, "Run Enclosed Region Detection");

            try
            {
                moveMesh.RunDetection((message, progress) =>
                {
                    EditorUtility.DisplayProgressBar("Enclosed Region Detection", message, progress);
                });
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.SetDirty(moveMesh);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private void ClearData()
        {
            var moveMesh = (MoveMesh)target;
            Undo.RecordObject(moveMesh, "Clear Enclosed Region Data");
            moveMesh.ClearData();
            EditorUtility.SetDirty(moveMesh);
            AssetDatabase.SaveAssetIfDirty(moveMesh);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private static VisualElement MakeDivider()
        {
            return new VisualElement
            {
                style =
                {
                    height = 1,
                    backgroundColor = new StyleColor(new Color(0.5f, 0.5f, 0.5f, 0.3f)),
                    marginBottom = 8,
                }
            };
        }

        private static Button MakeButton(string text)
        {
            return new Button
            {
                text = text,
                style =
                {
                    height = 30,
                    marginLeft = 0,
                    marginRight = 0,
                    unityFontStyleAndWeight = FontStyle.Bold,
                }
            };
        }
    }
}
