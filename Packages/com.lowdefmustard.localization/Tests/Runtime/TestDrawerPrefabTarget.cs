using UnityEngine;
using UnityEngine.Localization;

namespace LowDefMustard.Localization.Tests
{
    // Testing Notes:
    //  - This must be a. top-level (not nested inside a test script) && b. in Runtime Assembly (resolvable outside Editor)
    //  - Required for any prefab that gets saved to and reloaded from a prefab asset on disk
    
    public sealed class TestDrawerPrefabTarget : MonoBehaviour
    {
        [SimpleLocalizedString(TestTableType.ScratchAssetDrawer, true)] public LocalizedString localizedField;
    }
}
