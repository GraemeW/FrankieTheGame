using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Frankie.Stats;

namespace Frankie.Saving.Editor
{
    public class InactivePartySubCard : PartyBaseSubCard
    {
        public InactivePartySubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        protected override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not InactiveParty inactiveParty) { return; }
            
            HashSet<CharacterProperties> inactivePartyData = inactiveParty.ManualGetDataFromState(saveState);
            if (inactivePartyData == null)
            {
                subCardView.Add(new Label("No InactiveParty save data found"));
                return;
            }

            // Section 1 - Inactive Party Character Select
            var inactivePartyCharacters = new List<CharacterProperties>(inactivePartyData);
            var listContainer = new VisualElement();
            subCardView.Add(listContainer);

            var addButton = new Button { text = "+ Add Character", style = { width = standardButtonWidth } };
            subCardView.Add(addButton);
            DrawBasicPartyList(listContainer, inactiveParty, inactivePartyCharacters, () => ReconcileEntityView(inactivePartyCharacters));
            
            // Section 2 -- Party Entity View
            subCardView.Add(new Label("Party Entity View"));
            characterEntityContainer = new VisualElement();
            subCardView.Add(characterEntityContainer);
            
            if (saveableEntityCardData == null) { return; }
            ReconcileEntityView(inactivePartyCharacters);
            
            // Button Callbacks
            addButton.RegisterCallback<ClickEvent>(_ =>
            {
                inactivePartyCharacters.Add(null);
                saveState = inactiveParty.ManualGetStateFromData(inactivePartyCharacters.ToHashSet());
                RaiseSaveStateChanged();
                DrawBasicPartyList(listContainer, inactiveParty, inactivePartyCharacters, () => ReconcileEntityView(inactivePartyCharacters));
            });
        }
    }
}
