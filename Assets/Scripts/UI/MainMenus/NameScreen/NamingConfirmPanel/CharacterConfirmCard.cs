using UnityEngine;
using TMPro;

namespace Frankie.Menu.UI
{
    public class CharacterConfirmCard : MonoBehaviour
    {
        [SerializeField] private Transform characterStage;
        [SerializeField] private TextMeshProUGUI characterNameField;

        public void Setup(string characterName, GameObject thingPrefab)
        {
            if (!string.IsNullOrEmpty(characterName)) { characterNameField.text = characterName; }
            if (thingPrefab != null) { Instantiate(thingPrefab, characterStage); }
        }
    }
}
