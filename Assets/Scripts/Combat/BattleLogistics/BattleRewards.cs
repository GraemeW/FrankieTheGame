using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public class BattleRewards : MonoBehaviour
    {
        // Cached References
        BattleController battleController = null;

        private void Awake()
        {
            battleController = GetComponent<BattleController>();
        }

    }
}
