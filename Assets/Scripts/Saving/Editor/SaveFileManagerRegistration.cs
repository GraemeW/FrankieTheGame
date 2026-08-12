using UnityEditor;
using LowDefMustard.Saving;

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
