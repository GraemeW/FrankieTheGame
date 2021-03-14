using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Frankie.SceneManagement
{
    public class SceneLoader : MonoBehaviour
    {
        // Tunables
        [Header("Core Scene Listing")]
        [SerializeField] SceneReference splashScreen = null;
        [SerializeField] SceneReference startScreen = null;
        [SerializeField] SceneReference optionsScreen = null;
        [SerializeField] SceneReference newGame = null;

        [Header("Scene Parameters")]
        [SerializeField] float splashDelayTime = 2.5f;

        // State
        int currentSceneIndex = 0;

        private void Awake()
        {
            SetCurrentSceneIndex();
        }

        private void Start()
        {
            UnityEngine.Debug.Log(SceneManager.GetActiveScene().name);
            UnityEngine.Debug.Log(splashScreen.SceneName);

            if (SceneManager.GetActiveScene().name.Equals(splashScreen.SceneName))
            {
                StartCoroutine(SplashDelayToLoad());
            }
        }

        IEnumerator SplashDelayToLoad()
        {
            yield return new WaitForSeconds(splashDelayTime);
            LoadStartScreen();
        }

        public void LoadStartScreen()
        {
            SceneManager.LoadScene(startScreen.SceneName);
            SetCurrentSceneIndex();
        }

        public void LoadOptionsScreen()
        {
            SceneManager.LoadScene(optionsScreen.SceneName);
            SetCurrentSceneIndex();
        }

        public void LoadNewGame()
        {
            SceneManager.LoadScene(newGame.SceneName);
            SetCurrentSceneIndex();
        }

        IEnumerator LoadNewSceneAsync(SceneReference sceneReference)
        {
            yield return SceneManager.LoadSceneAsync(sceneReference.SceneName);
            SetCurrentSceneIndex();
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        private void SetCurrentSceneIndex()
        {
            currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        }
    }
}