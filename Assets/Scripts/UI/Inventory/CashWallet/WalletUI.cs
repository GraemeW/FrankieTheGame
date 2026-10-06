using UnityEngine;
using LowDefMustard.UIBox;
using Frankie.Core;

namespace Frankie.Inventory.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class WalletUI : MonoBehaviour
    {
        // State
        private WalletModel walletModel;

        #region UnityMethods
        private void Awake()
        {
            GameObject playerObject = Player.FindPlayerObject();
            if (playerObject == null || !playerObject.TryGetComponent(out Wallet wallet))
            {
                Destroy(gameObject);
                return;
            }

            walletModel = new WalletModel(wallet);
            GetComponent<UIToolkitMenuView>().SetDataSource(walletModel);
        }

        private void OnDestroy()
        {
            walletModel?.Dispose();
        }
        #endregion
    }
}
