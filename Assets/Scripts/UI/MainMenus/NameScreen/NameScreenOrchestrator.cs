using System.Collections;
using Frankie.Control;
using UnityEngine;

namespace Frankie.Menu.UI
{
    public class NameScreenOrchestrator : MonoBehaviour
    {
        // Tunables
        [Header("Controller")]
        [SerializeField] private MainMenuController mainMenuController;
        [Header("Character Walks")]
        [SerializeField] private UICharacter frankie;
        [SerializeField] private Transform offStagePosition;
        [SerializeField] private Transform stagePosition;
        [Header("InputDisplay")]
        [SerializeField] private InputDisplay inputDisplay;
        [Header("Keyboard")]
        [SerializeField] private Keyboard keyboard;

        private void Awake()
        {
            mainMenuController.AddInputReceiver(keyboard, null);
        }
        
        private void Start()
        {
            StartCoroutine(WalkRoutine());
            keyboard.Setup(inputDisplay);
        }

        private IEnumerator WalkRoutine()
        {
            yield return new WaitForSeconds(1f);
            frankie.MoveTowards(stagePosition.position);
            //yield return new WaitForSeconds(4f);
            //frankie.MoveTowards(offStagePosition.position);
        }
    }
}
