using TMPro;
using UnityEngine;

namespace Frankie.Menu.UI
{
    public class CreditsEntry : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleField;
        [SerializeField] private TMP_Text personField;

        public void SetTitle(string title)
        {
            if (titleField == null) { return; }
            titleField.SetText(title);
        }

        public void SetName(string person)
        {
            if (personField == null) { return;  }
            personField.SetText(person);
        }
    }
}
