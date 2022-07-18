using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Stats
{
    public class CharacterSpriteLink : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer = null;

        public SpriteRenderer GetSpriteRenderer() => spriteRenderer;
    }
}
