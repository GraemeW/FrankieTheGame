using UnityEngine;
using TMPro;
using Frankie.Core;

namespace Frankie.Inventory.UI
{
    public class WalletUI : MonoBehaviour
    {
        // Tunables
        [SerializeField] private TMP_Text walletField;

        // State
        private Wallet wallet;

        #region UnityMethods
        private void Awake()
        {
            SetupWallet();
        }

        private void OnEnable()
        {
            if (wallet == null) { return;}
            wallet.walletUpdated += RefreshUI;
        }

        private void OnDisable()
        {
            if (wallet == null) { return;}
            wallet.walletUpdated -= RefreshUI;
        }

        private void Start()
        {
            RefreshUI();
        }
        #endregion

        private void SetupWallet()
        {
            GameObject playerObject = Player.FindPlayerObject();
            if (playerObject == null || !playerObject.TryGetComponent(out wallet))
            {
                Destroy(gameObject);
                return;
            }
            
            wallet.walletUpdated += RefreshUI;
        }

        private void RefreshUI()
        {
            if (wallet == null) { return; }
            walletField.text = $"${wallet.GetCash():N0}";
        }
    }
}
