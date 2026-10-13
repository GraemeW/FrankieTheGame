using UnityEngine.UIElements;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class CreditsEntryElement : VisualElement
    {
        // Const Tunables
        private const string _ussClassName = "credits-entry";
        private const string _titleUssClassName = _ussClassName + "__title";
        private const string _personUssClassName = _ussClassName + "__person";

        // Cached References
        private readonly Label title;
        private readonly Label person;

        [UxmlAttribute] public string titleText
        {
            get => title.text;
            set => title.text = value;
        }

        [UxmlAttribute] public string personName
        {
            get => person.text;
            set => person.text = value;
        }

        public CreditsEntryElement()
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;

            title = new Label { pickingMode = PickingMode.Ignore };
            title.AddToClassList(_titleUssClassName);
            Add(title);

            person = new Label { pickingMode = PickingMode.Ignore };
            person.AddToClassList(_personUssClassName);
            Add(person);
        }
    }
}
