using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using LowDefMustard.Core;
using LowDefMustard.Saving;
using LowDefMustard.Saving.Editor;
using LowDefMustard.Zones;

namespace Frankie.Saving.Editor
{
    [InitializeOnLoad]
    public class SceneSelector
    {
        private const float _largeButtonWidth = 250f;

        static SceneSelector()
        {
            SaveEditor.sceneSelectorFactory = CreateSceneSelector;
        }

        private static VisualElement CreateSceneSelector(SceneSelectorContext context)
        {
            var container = new VisualElement();

            string currentLastScene = SavingSystem.ManualGetLastScene(context.SaveState);
            Zone lastZone = Zone.GetFromName(currentLastScene);

            var zoneField = new ObjectField { objectType = typeof(Zone), value = lastZone, style = { width = _largeButtonWidth } };
            zoneField.SetEnabled(context.SaveState != null);
            container.Add(zoneField);

            var openSceneButton = new Button { text = "Open Scene", style = { width = _largeButtonWidth } };
            openSceneButton.SetEnabled(lastZone != null);
            container.Add(openSceneButton);

            zoneField.RegisterValueChangedCallback(changeEvent =>
            {
                Zone testZone = changeEvent.newValue as Zone;
                string testSceneName = string.Empty;
                if (testZone != null)
                {
                    SceneReference sceneReference = testZone.GetSceneReference();
                    if (!string.IsNullOrEmpty(sceneReference.SceneName)) { testSceneName = sceneReference.SceneName; }
                }

                if (testSceneName == string.Empty)
                {
                    zoneField.SetValueWithoutNotify(changeEvent.previousValue as Zone);
                    return;
                }

                lastZone = testZone;
                openSceneButton.SetEnabled(lastZone != null);
                SavingSystem.ManualUpdateLastScene(context.SaveState, testSceneName);
                context.OnSceneDataChanged();

                LDMLog.Warning(LDMLogCategory.Saving, $"Updated the save's last scene to '{lastZone}'. Check the player's saved position matches the new scene.");
            });

            openSceneButton.RegisterCallback<ClickEvent>(_ =>
            {
                if (lastZone == null) { return; }
                if (SceneManager.GetActiveScene().name == lastZone.GetSceneReference().SceneName) { return; }
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }

                string scenePath = lastZone.GetSceneReference().GetScenePath();
                if (string.IsNullOrEmpty(scenePath))
                {
                    LDMLog.Warning(LDMLogCategory.Saving, $"No scene found for the save's last zone '{lastZone}'.");
                    return;
                }

                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                context.OnReloadRequested();
            });

            return container;
        }
    }
}
