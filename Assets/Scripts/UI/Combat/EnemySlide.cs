using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Combat.UI
{
    public class EnemySlide : MonoBehaviour
    {
        // Tunables
        [SerializeField] Image image = null;

        public void UpdateImage(Sprite sprite)
        {
            image.sprite = sprite;
        }
    }
}
