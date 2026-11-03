using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace LowDefMustard.Core.Editor
{
    internal sealed class LDMLogSettingsBuildCheck : IPreprocessBuildWithReport
    {
        // Builds only see the settings through Preloaded Assets -> re-add them if they've dropped off the list

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            LDMLogSettings settings = LDMLogSettingsMenu.GetRegisteredSettings();
            if (settings == null || LDMLogSettingsMenu.IsPreloaded(settings)) { return; }

            LDMLogSettingsMenu.Register(settings);
            LDMLog.Warning(LDMLogCategory.Core, $"{settings.name} was missing from Player Settings > Preloaded Assets ~ re-added it for this build");
        }
    }
}
