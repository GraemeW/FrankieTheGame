using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    public class Shopper : MonoBehaviour
    {
        // State
        Shop currentShop = null;

        public void SetShop(Shop shop)
        {
            currentShop = shop;
        }

        public Shop GetCurrentShop()
        {
            return currentShop;
        }
    }
}
