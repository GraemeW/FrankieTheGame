namespace Frankie.ZoneManagement.Editor
{
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(MoveMesh))]
    public class EnclosedRegionFinderEditor : Editor
    {
        private const string _statusLabelName = "status-label";
        
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
            runButton.RegisterCallback<ClickEvent>(_ => RunDetection(root));
            root.Add(runButton);

            Button clearButton = MakeButton("Clear Data");
            clearButton.RegisterCallback<ClickEvent>(_ => ClearData(root));
            root.Add(clearButton);

            Label statusLabel = MakeLabel(_statusLabelName);
            root.Add(statusLabel);

            RefreshStatusLabel(root);
            return root;
        }

        private void RunDetection(VisualElement root)
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
            RefreshStatusLabel(root);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private void ClearData(VisualElement root)
        {
            var moveMesh = (MoveMesh)target;
            Undo.RecordObject(moveMesh, "Clear Enclosed Region Data");
            moveMesh.ClearData();
            EditorUtility.SetDirty(moveMesh);
            AssetDatabase.SaveAssetIfDirty(moveMesh);
            RefreshStatusLabel(root);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private void RefreshStatusLabel(VisualElement root)
        {
            var label = root?.Q<Label>();
            if (label == null) { return; }
            var finder = (MoveMesh)target;
            int count = finder.enclosedRegions?.Count ?? 0;
            label.text = count == 0 ? "No regions detected — press Run Detection." : $"{count} enclosed region{(count == 1 ? "" : "s")} found.";
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

        private static Label MakeLabel(string name)
        {
            return new Label
            {
                name = name,
                style =
                {
                    marginTop = 4,
                    unityTextAlign  = TextAnchor.MiddleCenter,
                    color = new StyleColor(new Color(0.6f, 0.6f, 0.6f)),
                    whiteSpace = WhiteSpace.Normal,
                }
            };
        }
    }
}
