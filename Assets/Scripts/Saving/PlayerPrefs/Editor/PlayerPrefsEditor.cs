#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Frankie.Saving;

namespace Frankie.Utils.Editor
{
    public class PlayerPrefsEditorWindow : EditorWindow
    {
        // State
        private readonly List<PrefsEntryData> entries = new();
        private List<PrefsKeyInfo> availableKeysToAdd = new();
        private ScrollView rowContainer;
        private PopupField<PrefsKeyInfo> keyDropdown;
        private Button addButton;

        [MenuItem("Tools/PlayerPrefs Editor", false, 305)]
        public static void ShowWindow()
        {
            var window = GetWindow<PlayerPrefsEditorWindow>();
            window.titleContent = new GUIContent("PlayerPrefs Editor");
            window.minSize = new Vector2(460, 300);
        }

        #region UnityMethods
        public void CreateGUI()
        {
            RefreshEntries();
            BuildLayout();
        }
        #endregion

        #region DataRefresh
        private void RefreshEntries()
        {
            entries.Clear();
            entries.AddRange(PlayerPrefsController.GetPrefsEntries());
            RefreshAvailableKeys();
        }

        private void RefreshAvailableKeys()
        {
            availableKeysToAdd = PlayerPrefsController.GetAvailableKeysToAdd();

            if (keyDropdown == null) { return; }

            keyDropdown.choices = availableKeysToAdd;
            bool hasChoices = availableKeysToAdd.Count > 0;
            keyDropdown.SetEnabled(hasChoices);
            addButton?.SetEnabled(hasChoices);
            if (hasChoices) { keyDropdown.value = availableKeysToAdd[0]; }
        }
        #endregion

        #region Layout
        private void BuildLayout()
        {
            InitializeRoot(rootVisualElement);

            rootVisualElement.Add(MakeHeader());

            rowContainer = new ScrollView { style = { flexGrow = 1 } };
            rootVisualElement.Add(rowContainer);

            RebuildRows();

            rootVisualElement.Add(MakeSeparator());
            rootVisualElement.Add(MakeToolbar());
        }

        private VisualElement MakeToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.Add(MakeAddEntryRow());
            toolbar.Add(MakeAdminRow());
            return toolbar;
        }

        private VisualElement MakeAddEntryRow()
        {
            var addRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 } };

            PrefsKeyInfo initialSelection = availableKeysToAdd.Count > 0 ? availableKeysToAdd[0] : default;
            keyDropdown = new PopupField<PrefsKeyInfo>(availableKeysToAdd, initialSelection, FormatKeyChoice, FormatKeyChoice)
            {
                style = { width = 220, marginRight = 4 }
            };
            keyDropdown.SetEnabled(availableKeysToAdd.Count > 0);
            addButton = MakeStandardButton("Add Entry");
            addButton.SetEnabled(availableKeysToAdd.Count > 0);
            addButton.RegisterCallback<ClickEvent>(_ => AddNewEntry());

            addRow.Add(keyDropdown);
            addRow.Add(addButton);
            return addRow;
        }

        private VisualElement MakeAdminRow()
        {
            var adminRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd } };

            Button refreshButton = MakeStandardButton("Refresh");
            refreshButton.RegisterCallback<ClickEvent>(_ => RefreshAll());
            Button saveButton = MakeStandardButton("Save to PlayerPrefs", 6);
            saveButton.RegisterCallback<ClickEvent>(_ => SaveAll());
            Button clearButton = MakeStandardButton("Clear All PlayerPrefs", 6);
            clearButton.RegisterCallback<ClickEvent>(_ => ClearAllPrefs());

            adminRow.Add(refreshButton);
            adminRow.Add(saveButton);
            adminRow.Add(clearButton);
            return adminRow;
        }

        private static string FormatKeyChoice(PrefsKeyInfo info) => string.IsNullOrEmpty(info.key) ? "No keys available" : $"{info.key} ({info.type})";
        #endregion

        #region Rows
        private void RebuildRows()
        {
            rowContainer.Clear();
            foreach (PrefsEntryData entry in entries) { rowContainer.Add(BuildRow(entry)); }
        }

        private VisualElement BuildRow(PrefsEntryData entry)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };

            VisualElement keyCell = MakeCell(_columns[0]);
            keyCell.Add(new Label(entry.key));
            row.Add(keyCell);

            VisualElement typeCell = MakeCell(_columns[1]);
            typeCell.Add(new Label(entry.type.ToString()));
            row.Add(typeCell);

            VisualElement valueCell = MakeCell(_columns[2]);
            valueCell.Add(MakeValueField(entry));
            row.Add(valueCell);

            VisualElement deleteCell = MakeCell(_columns[3]);
            var deleteButton = new Button { text = "-", style = { flexGrow = 1 } };
            deleteButton.RegisterCallback<ClickEvent>(_ => DeleteEntry(entry));
            deleteCell.Add(deleteButton);
            row.Add(deleteCell);

            return row;
        }

        private static VisualElement MakeValueField(PrefsEntryData entry)
        {
            switch (entry.type)
            {
                case PrefsValueType.Int:
                    IntegerField intField = MakeStandardIntField(entry.value);
                    intField.RegisterValueChangedCallback(evt => entry.SetValue(evt.newValue.ToString(CultureInfo.InvariantCulture)));
                    return intField;

                case PrefsValueType.Float:
                    FloatField floatField = MakeStandardFloatField(entry.value);
                    floatField.RegisterValueChangedCallback(evt => entry.SetValue(evt.newValue.ToString(CultureInfo.InvariantCulture)));
                    return floatField;

                default:
                    TextField stringField = MakeStandardTextField(entry.value);
                    stringField.RegisterValueChangedCallback(evt => entry.SetValue(evt.newValue));
                    return stringField;
            }
        }
        #endregion

        #region Actions
        private void AddNewEntry()
        {
            if (availableKeysToAdd.Count == 0) { return; }

            PrefsKeyInfo selected = keyDropdown.value;
            string defaultValue = selected.type == PrefsValueType.String ? string.Empty : "0";
            entries.Add(new PrefsEntryData(selected.key, selected.type, defaultValue));

            RefreshAvailableKeys();
            RebuildRows();
        }

        private void DeleteEntry(PrefsEntryData entry)
        {
            PlayerPrefsController.DeleteKey(entry.key);
            PlayerPrefs.Save();

            entries.Remove(entry);
            RefreshAvailableKeys();
            RebuildRows();
        }

        private void SaveAll()
        {
            foreach (PrefsEntryData entry in entries) { PlayerPrefsController.SetPref(entry); }
            PlayerPrefs.Save();

            RefreshEntries();
            RebuildRows();
        }

        private void RefreshAll()
        {
            RefreshEntries();
            RebuildRows();
        }

        private void ClearAllPrefs()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Clear All PlayerPrefs",
                "This will permanently delete every PlayerPrefs entry for this project. Continue?",
                "Clear All",
                "Cancel");
            if (!confirmed) { return; }

            PlayerPrefsController.ClearPlayerPrefs();
            RefreshEntries();
            RebuildRows();
        }
        #endregion

        #region StaticUIBuilders
        private static void InitializeRoot(VisualElement root)
        {
            root.Clear();
            root.style.paddingLeft = 6;
            root.style.paddingRight = 6;
            root.style.paddingTop = 6;
            root.style.paddingBottom = 6;
        }
        
        private readonly struct ColumnSpec
        {
            public readonly string header;
            public readonly float flexGrow;
            public readonly float fixedWidth;

            public ColumnSpec(string header, float flexGrow, float fixedWidth = -1f)
            {
                this.header = header;
                this.flexGrow = flexGrow;
                this.fixedWidth = fixedWidth;
            }
        }

        private static readonly ColumnSpec[] _columns =
        {
            new("Key", 2f),
            new("Type", 0f, 80f),
            new("Value", 2f),
            new("", 0f, 24f)
        };

        private static VisualElement MakeHeader()
        {
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };
            foreach (ColumnSpec column in _columns)
            {
                VisualElement cell = MakeCell(column);
                cell.Add(new Label(column.header) { style = { unityFontStyleAndWeight = FontStyle.Bold } });
                header.Add(cell);
            }
            return header;
        }

        private static VisualElement MakeCell(ColumnSpec spec)
        {
            var cell = new VisualElement { style = { marginRight = 4 } };
            if (spec.fixedWidth >= 0f) { cell.style.width = spec.fixedWidth; }
            else { cell.style.flexGrow = spec.flexGrow; }
            return cell;
        }

        private static VisualElement MakeSeparator()
        {
            return new VisualElement
            {
                style =
                {
                    height = 1,
                    marginTop = 4,
                    marginBottom = 4,
                    backgroundColor = new Color(0f, 0f, 0f, 0.2f)
                }
            };
        }

        private static Button MakeStandardButton(string text, float offset = 0)
        {
            return new Button { text = text, style = { marginLeft = offset } };
        }

        private static IntegerField MakeStandardIntField(string value)
        {
            return new IntegerField
            {
                value = int.TryParse(value, out int i) ? i : 0,
                style = { flexGrow = 1 }
            };
        }

        private static FloatField MakeStandardFloatField(string value)
        {
            return new FloatField
            {
                value = float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : 0f,
                style = { flexGrow = 1 }
            };
        }

        private static TextField MakeStandardTextField(string value)
        {
            return new TextField
            {
                value = value ?? string.Empty,
                style = { flexGrow = 1 }
            };
        }
        #endregion
    }
}
#endif
