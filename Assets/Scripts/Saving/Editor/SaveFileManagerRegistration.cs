using UnityEditor;
using LowDefMustard.Saving.Editor;

namespace Frankie.Saving.Editor
{
    [InitializeOnLoad]
    public static class SaveFileManagerRegistration
    {
        static SaveFileManagerRegistration()
        {
            SaveFileManagerProvider.current = new SavingFileManagerAdapter();
        }
    }
}
