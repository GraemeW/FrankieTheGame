namespace LowDefMustard.Localization.Tests.Editor
{
    // Test-only stand-ins for a project's real table-type enum + LocalizationToolBase<T> alias
    internal enum TestTableType
    {
        Core,
        UI,
        ScratchAsset
    }

    internal sealed class TestLocalizationTool : LocalizationToolBase<TestTableType> { }
}
