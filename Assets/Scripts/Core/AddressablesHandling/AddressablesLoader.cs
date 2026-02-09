using UnityEngine;
using Frankie.Combat;
using Frankie.Inventory;
using Frankie.Quests;
using Frankie.Stats;
using Frankie.ZoneManagement;

namespace Frankie.Core
{
    public class AddressablesLoader : MonoBehaviour
    {
        // State
        private bool isSingleton = false;

        #region UnityMethods
        private void Awake()
        {
            VerifySingleton();

            BattleAction.BuildCacheIfEmpty();
            CharacterProperties.BuildCacheIfEmpty();
            InventoryItem.BuildCacheIfEmpty();
            Quest.BuildCacheIfEmpty();
            Skill.BuildCacheIfEmpty();
            Zone.BuildCacheIfEmpty();
        }

        private void OnDestroy()
        {
            if (!isSingleton) { return; }
            
            BattleAction.ReleaseCache();
            CharacterProperties.ReleaseCache();
            InventoryItem.ReleaseCache();
            Quest.ReleaseCache();
            Skill.ReleaseCache();
            Zone.ReleaseCache();
        }
        #endregion

        #region PrivateMethods
        private void VerifySingleton()
        {
            // Singleton through standard approach -- do not use persistent object spawner for addressables loader
            // i.e. want assets loaded in Awake
            int numberOfPlayers = FindObjectsByType<AddressablesLoader>(FindObjectsSortMode.None).Length;
            if (numberOfPlayers > 1)
            {
                isSingleton = false;
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
            else
            {
                isSingleton = true;
                gameObject.transform.parent = null;
                DontDestroyOnLoad(gameObject);
            }
        }
        #endregion
    }
}
