using System;
using System.Collections.Generic;

namespace LowDefMustard.Saving
{
    public interface ISaveFileManager
    {
        event Action gameListUpdated;
        string GetCurrentSaveName();
        bool HasSave(string saveName);
        IEnumerable<string> ListSaves(bool includeSession = true);
        void SetCurrentSave(string saveName, bool announceGameListUpdate = true);
        void CopySave(string newSave, bool announceGameListUpdate = true);
        void CopySave(string existingSaveName, string newSaveName, bool announceGameListUpdate = true);
        void Delete(bool announceGameListUpdate = true);
        void Delete(string saveName, bool announceGameListUpdate = true);
        string GetSaveNameForIndex(int index);
        bool GetInfoFromSave(string saveName, out string characterName, out int level);
    }
}
