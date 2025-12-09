using Frankie.ZoneManagement;
using UnityEngine;

namespace Frankie.Control.Specialization
{
    public class WorldFaderInterface : MonoBehaviour
    {
        [SerializeField] private float blipFadeHoldSeconds = 1.0f;
        
        public void BlipFade()
        {
            Fader fader = Fader.FindFader();
            fader.StartBlipFade(blipFadeHoldSeconds);
        }
    }
}
