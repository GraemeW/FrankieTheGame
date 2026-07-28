using UnityEngine;

namespace Frankie.Utils
{
    public class FrankieDemoToggler : MonoBehaviour
    {
        [SerializeField] private bool executeIfDemo = false;
        [SerializeField] private DemoToggleType demoToggleType;
        [SerializeField] private Transform entitiesToEnable;
        
        private void Start()
        {
            if (executeIfDemo != FrankieDebugger.IsDemo()) { return; }
            
            switch (demoToggleType)
            {
                case DemoToggleType.Disable:
                    gameObject.SetActive(false);
                    break;
                case DemoToggleType.Destroy:
                    Destroy(gameObject);
                    break;
                case DemoToggleType.EnableFromTransform:
                case DemoToggleType.DisableFromTransform:
                    if (entitiesToEnable == null) { return; }
                    foreach (GameObject entity in entitiesToEnable)
                    {
                        entity.SetActive(demoToggleType == DemoToggleType.EnableFromTransform);
                    }
                    break;
            }
        }
    }
}
