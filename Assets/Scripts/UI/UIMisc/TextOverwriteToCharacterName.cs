using System.Collections;
using UnityEngine;
using TMPro;
using Frankie.Stats;

namespace Frankie.Utils.UI
{
    public class TextOverwriteToCharacterName : MonoBehaviour
    {
        // Tunables
        [SerializeField] private TextMeshProUGUI characterNameField;
        [SerializeField] private CharacterProperties characterProperties;

        // State
        private Coroutine textOverwriteCoroutine;
        
        private void OnEnable()
        { 
            if (characterNameField == null || characterProperties == null) { return; }

            // Repaint after a frame to ensure UI elements ready for updates
            if (textOverwriteCoroutine != null) { StopCoroutine(textOverwriteCoroutine); }
            textOverwriteCoroutine = StartCoroutine(TriggerTextOverwrite());
        }

        private void OnDisable()
        {
            if (textOverwriteCoroutine != null) { StopCoroutine(textOverwriteCoroutine); }
        }

        private IEnumerator TriggerTextOverwrite()
        {
            characterNameField.SetText(CharacterProperties.GetCharacterDisplayName(characterProperties));
            yield return null;
            characterNameField.SetText(CharacterProperties.GetCharacterDisplayName(characterProperties));
        }
    }
}
