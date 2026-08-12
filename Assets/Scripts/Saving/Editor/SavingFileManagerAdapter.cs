using System;
using System.Collections.Generic;
using LowDefMustard.Saving;

namespace Frankie.Saving.Editor
{
    public class SavingFileManagerAdapter : ISaveFileManager
    {
        public event Action gameListUpdated
        {
            add => SaveFileManager.gameListUpdated += value;
            remove => SaveFileManager.gameListUpdated -= value;
        }
        public string GetCurrentSaveName() => SaveFileManager.GetCurrentSaveName();
        public bool HasSave(string saveName) => SaveFileManager.HasSave(saveName);
        public IEnumerable<string> ListSaves(bool includeSession = true) => SaveFileManager.ListSaves(includeSession);
        public void SetCurrentSave(string saveName, bool announce = true) => SaveFileManager.SetCurrentSave(saveName, announce);
        public void CopySave(string newSave, bool announce = true) => SaveFileManager.CopySave(newSave, announce);
        public void CopySave(string existingSaveName, string newSaveName, bool announce = true) => SaveFileManager.CopySave(existingSaveName, newSaveName, announce);
        public void Delete(bool announce = true) => SaveFileManager.Delete(announce);
        public void Delete(string saveName, bool announce = true) => SaveFileManager.Delete(saveName, announce);
        public string GetSaveNameForIndex(int index) => SaveFileManager.GetSaveNameForIndex(index);
        public bool GetInfoFromSave(string saveName, out string characterName, out int level) => SaveFileManager.GetInfoFromSave(saveName, out characterName, out level);
    }
}
