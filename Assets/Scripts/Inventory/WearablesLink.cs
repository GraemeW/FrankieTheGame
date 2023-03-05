using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    public class WearablesLink : MonoBehaviour, IModifierProvider
    {
        [SerializeField] Transform attachedObjectsRoot = null;

        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            if (attachedObjectsRoot == null) { yield break; }

            foreach (Transform wearableObject in attachedObjectsRoot)
            {
                if (!wearableObject.TryGetComponent(out IModifierProvider modifierProvider)) { yield break; }

                foreach (float modifier in modifierProvider.GetAdditiveModifiers(stat))
                {
                    yield return modifier;
                }
            }
        }
    }
}
