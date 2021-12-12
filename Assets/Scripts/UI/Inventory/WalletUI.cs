using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Frankie.Inventory.UI
{
    public class WalletUI : MonoBehaviour
    {
        // Tunables
        [SerializeField] TMP_Text walletField = null;

        // State
        Wallet wallet = null;

        #region UnityMethods
        private void Awake()
        {
            SetupWallet();
        }

        private void OnEnable()
        {
            if (wallet != null)
            {
                wallet.walletUpdated += RefreshUI;
            }
        }

        private void OnDisable()
        {
            if (wallet != null)
            {
                wallet.walletUpdated -= RefreshUI;
            }
        }

        private void Start()
        {
            RefreshUI();
        }
        #endregion

        private void SetupWallet()
        {
            wallet = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Wallet>();
            if (wallet == null) { Destroy(gameObject); }

            wallet.walletUpdated += RefreshUI;
        }

        private void RefreshUI()
        {
            if (wallet == null) { return; }

            walletField.text = $"${wallet.GetCash():N0}";
        }
    }
}
