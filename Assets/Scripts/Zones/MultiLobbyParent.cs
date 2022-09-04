using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.ZoneManagement
{
    public class MultiLobbyParent : MonoBehaviour
    {
        private void Awake()
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

}
