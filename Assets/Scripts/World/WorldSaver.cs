using UnityEngine;
using Frankie.Core;

namespace Frankie.Control.Specialization
{
    public class WorldSaver : MonoBehaviour
    {
        public void Save()
        {
            SavingWrapper.Save();
        }
    }
}
