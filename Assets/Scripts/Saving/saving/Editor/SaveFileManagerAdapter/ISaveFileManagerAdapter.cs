using System;
using System.Collections.Generic;

namespace LowDefMustard.Saving.Editor
{
    public interface ISaveFileManagerAdapter
    {
        // SaveFileManager should implement below methods, but since it is static, it cannot inherit an interface
        // The adapter layer is used to translate the relevant per-project SaveFileManager capability
        
        event Action gameListUpdated;
        string GetCurrentSaveName();
        string GetSaveNameForIndex(int index);
        bool HasSave(string saveName);
        bool GetInfoFromSave(string saveName, out string characterName, out int level);
        IEnumerable<string> ListSaves(bool includeSession = true);
        void SetCurrentSave(string saveName, bool announceGameListUpdate = true);
        void CopySave(string newSave, bool announceGameListUpdate = true);
        void CopySave(string existingSaveName, string newSaveName, bool announceGameListUpdate = true);
        void Delete(bool announceGameListUpdate = true);
        void Delete(string saveName, bool announceGameListUpdate = true);
    }
}
