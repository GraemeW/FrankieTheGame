using Frankie.ZoneManagement;
using UnityEngine;

namespace Frankie.Control
{
    public class SplashMenuController : MonoBehaviour
    {
        // Tunables
        [Header("Input Parameters")]
        [SerializeField] string interactButtonOne = "Fire1";
        [SerializeField] string interactButtonTwo = "Fire2";
        [SerializeField] KeyCode interactKeyOne = KeyCode.E;
        [SerializeField] KeyCode interactKeyTwo = KeyCode.Escape;
        [Header("Scene Parameters")]
        [SerializeField] float splashDelayTime = 15.0f;

        // State
        Coroutine skipSplashRoutine = null;

        // Cached References
        SceneLoader sceneLoader = null;

        private void Start()
        {
            sceneLoader = GameObject.FindGameObjectWithTag("SceneLoader").GetComponent<SceneLoader>();
                // SceneLoader is a persistent object, thus can only be found after Awake -- so find in Start
            skipSplashRoutine = StartCoroutine(sceneLoader.SplashDelayToLoad(splashDelayTime));
        }

        private void Update()
        {
            if (Input.GetButtonDown(interactButtonOne) || Input.GetButtonDown(interactButtonTwo)
                || Input.GetKeyDown(interactKeyOne) || Input.GetKeyDown(interactKeyTwo))
            {
                if (skipSplashRoutine != null) { StopCoroutine(skipSplashRoutine); skipSplashRoutine = null; }
                sceneLoader.QueueStartScreen();
            }
        }
    }

}
