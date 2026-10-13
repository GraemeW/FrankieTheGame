using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;
using Frankie.Utils.UI;

namespace Frankie.Combat.UI
{
    [UxmlElement]
    public sealed partial class CharacterSlideElement : Button
    {
        // Data sources:
        //  - CharacterSlideModel (dataSource) for slide state
        //  - CombatParticipantModel (characterDataSource) for character values
        
        // Const Tunables
        private const string _ussClassName = "character-slide";
        private const string _selectedUssClassName = _ussClassName + "--selected";
        private const string _targetUssClassName = _ussClassName + "--target";
        private const string _deadUssClassName = _ussClassName + "--dead";
        private const string _backingUssClassName = _ussClassName + "__backing";
        private const string _frameUssClassName = _ussClassName + "__frame";
        private const string _namePaneUssClassName = _ussClassName + "__name-pane";
        private const string _nameUssClassName = _ussClassName + "__name";
        private const string _statsPaneUssClassName = _ussClassName + "__stats-pane";
        private const string _statLineUssClassName = _ussClassName + "__stat-line";
        private const string _statLabelUssClassName = _ussClassName + "__stat-label";
        private const string _blackBarUssClassName = _ussClassName + "__black-bar";
        private const string _cooldownUssClassName = _ussClassName + "__cooldown";
        private const string _statusEffectsUssClassName = _ussClassName + "__status-effects";
        private const string _damageTextUssClassName = _ussClassName + "__damage-text";
        private const string _textDisplayUssClassName = "text-display";

        // State
        private CharacterSlideState internalSlideState = CharacterSlideState.Ready;
        private CharacterSlideModel subscribedModel;

        // Cached References
        private readonly DamageTextSpawnerElement damageTextSpawner;
        private readonly SlideEffects slideEffects;
        private readonly List<VisualElement> characterBoundElements = new();
        private readonly Label nameLabel;
        private readonly Label hpStatLabel;
        private readonly Label apStatLabel;
        private readonly StatDigitsElement hpDigits;
        private readonly StatDigitsElement apDigits;

        // Note: UxmlAttributes exist for UI Builder previews only - bound model values override them at runtime
        [CreateProperty, UxmlAttribute] public CharacterSlideState slideState
        {
            get => internalSlideState;
            set
            {
                internalSlideState = value;
                ApplyHighlight();
            }
        }

        [UxmlAttribute] public string nameText
        {
            get => nameLabel.text;
            set => nameLabel.text = value;
        }

        [UxmlAttribute] public string hpLabelText
        {
            get => hpStatLabel.text;
            set => hpStatLabel.text = value;
        }

        [UxmlAttribute] public string apLabelText
        {
            get => apStatLabel.text;
            set => apStatLabel.text = value;
        }

        [UxmlAttribute] public float hpValue
        {
            get => float.IsNaN(hpDigits.value) ? 0f : hpDigits.value;
            set => hpDigits.value = value;
        }

        [UxmlAttribute] public float apValue
        {
            get => float.IsNaN(apDigits.value) ? 0f : apDigits.value;
            set => apDigits.value = value;
        }

        public object characterDataSource
        {
            set { foreach (VisualElement characterBoundElement in characterBoundElements) { characterBoundElement.dataSource = value; } }
        }

        public CharacterSlideElement()
        {
            focusable = false; // Keyboard/gamepad input handled via IInputReceiver - avoid UITK navigation double-firing
            AddToClassList(_ussClassName);

            AddPart(new VisualElement(), _backingUssClassName);

            FrameElement namePane = AddPart(new FrameElement(), _namePaneUssClassName);
            nameLabel = AsCharacterBound(CreateBoundLabel(nameof(CombatParticipantModel.characterName), _nameUssClassName));
            namePane.Add(nameLabel);

            FrameElement statsPane = AddPart(new FrameElement(), _statsPaneUssClassName);
            statsPane.Add(CreateStatLine(nameof(CharacterSlideModel.hpLabel), nameof(CombatParticipantModel.hp), out hpStatLabel, out hpDigits));
            statsPane.Add(CreateStatLine(nameof(CharacterSlideModel.apLabel), nameof(CombatParticipantModel.ap), out apStatLabel, out apDigits));

            AddPart(new VisualElement(), _blackBarUssClassName);
            AddPart(new FrameElement(), _frameUssClassName);
            damageTextSpawner = AddPart(new DamageTextSpawnerElement(nameof(CharacterSlideModel.hitMissText), nameof(CharacterSlideModel.hitCritText)), _damageTextUssClassName);
            AddPart(new CooldownTimerElement(nameof(CharacterSlideModel.cooldown)), _cooldownUssClassName);
            AsCharacterBound(AddPart(new StatusEffectPanelElement(nameof(CombatParticipantModel.statusEffects)), _statusEffectsUssClassName));

            slideEffects = new SlideEffects(this, false); // Horizontal grow only

            SetBinding(nameof(slideState), UIToolkitBindings.ToTarget(nameof(CharacterSlideModel.slideState)));
            RegisterCallback<AttachToPanelEvent>(_ => SubscribeToModel(dataSource as CharacterSlideModel));
            RegisterCallback<DetachFromPanelEvent>(_ => SubscribeToModel(null));
        }

        #region ElementConstruction
        private T AddPart<T>(T part, string partUssClassName) where T : VisualElement
        {
            part.AddToClassList(partUssClassName);
            part.pickingMode = PickingMode.Ignore;
            Add(part);
            return part;
        }

        private T AsCharacterBound<T>(T characterBoundElement) where T : VisualElement
        {
            characterBoundElements.Add(characterBoundElement);
            return characterBoundElement;
        }

        private static Label CreateBoundLabel(string sourcePropertyName, string labelUssClassName)
        {
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.AddToClassList(labelUssClassName);
            label.SetBinding(nameof(Label.text), UIToolkitBindings.ToTarget(sourcePropertyName));
            return label;
        }

        private VisualElement CreateStatLine(string labelSourcePropertyName, string valueSourcePropertyName, out Label statLabel, out StatDigitsElement statDigits)
        {
            var statLine = new VisualElement { pickingMode = PickingMode.Ignore };
            statLine.AddToClassList(_statLineUssClassName);

            statLabel = CreateBoundLabel(labelSourcePropertyName, _statLabelUssClassName);
            statLabel.AddToClassList(_textDisplayUssClassName);
            statDigits = AsCharacterBound(new StatDigitsElement(valueSourcePropertyName));
            statLine.Add(statLabel);
            statLine.Add(statDigits);
            return statLine;
        }
        #endregion

        #region Highlight
        // Note:  Highlight colours live in USS - the state classes set --frame-tint-override for the frames within
        private void ApplyHighlight()
        {
            EnableInClassList(_selectedUssClassName, internalSlideState == CharacterSlideState.Selected);
            EnableInClassList(_targetUssClassName, internalSlideState == CharacterSlideState.Target);
            EnableInClassList(_deadUssClassName, internalSlideState == CharacterSlideState.Dead);
        }
        #endregion

        #region Effects
        private void SubscribeToModel(CharacterSlideModel model)
        {
            if (subscribedModel != null) { subscribedModel.damageTextQueued -= damageTextSpawner.Enqueue; }
            subscribedModel = model;
            if (subscribedModel != null) { subscribedModel.damageTextQueued += damageTextSpawner.Enqueue; }
            slideEffects.Subscribe(subscribedModel);
        }
        #endregion
    }
}
