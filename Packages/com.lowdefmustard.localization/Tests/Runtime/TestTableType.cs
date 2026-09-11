namespace LowDefMustard.Localization.Tests
{
    // Test-only stand-ins for a project's real table-type enum + LocalizationToolBase<T> alias
    // Each Scratch* value is reserved for one specific test file's table/collection (i.e. to avoid collisions)
    public enum TestTableType
    {
        Core,
        UI,
        ScratchAsset,
        ScratchAssetEditor,
        ScratchAssetLocalizable,
        ScratchAssetDeletion,
        ScratchAssetDrawer
    }
}