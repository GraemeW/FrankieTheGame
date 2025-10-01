using Frankie.Core;
using UnityEngine;

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