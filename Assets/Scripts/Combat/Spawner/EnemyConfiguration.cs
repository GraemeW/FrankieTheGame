using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat.Spawner
{
    [System.Serializable]
    public class EnemyConfiguration
    {
        [SerializeField][Min(0)] public int minimum = 0;
        [SerializeField][Min(0)] public int maximum = 1;
        [SerializeField] public CharacterProperties characterProperties = null;
    }
}
