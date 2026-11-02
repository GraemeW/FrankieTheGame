using System.Collections.Generic;

namespace LowDefMustard.Rendering.Tests
{
    internal sealed class TestDisplaySettingMonitor : DisplaySettingMonitorBase
    {
        public bool isConfigured = true;
        public readonly List<ResolutionSetting> savedSettings = new();

        protected override bool IsResolutionAlreadyConfigured() => isConfigured;
        protected override void SaveResolutionSetting(ResolutionSetting resolutionSetting) => savedSettings.Add(resolutionSetting);
    }
}
