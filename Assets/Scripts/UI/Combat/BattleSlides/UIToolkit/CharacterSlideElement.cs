using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
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

        // Const Tunables - Effects
        private const float _damageShakeMagnitude = 10f;
        private const float _criticalDamageShakeMultiplier = 2f;
        private const float _shakeDuration = 0.4f;
        private const int _shakeCount = 4;
        private const float _dimmingMin = 0.7f;
        private const float _halfDimmingTime = 0.1f;
        private const float _growingMax = 1.2f;
        private const float _halfGrowingTime = 0.125f;

        // State
        private CharacterSlideState internalSlideState = CharacterSlideState.Ready;
        private CharacterSlideModel subscribedModel;
        private float shakeTime = Mathf.Infinity;
        private float shakeStepTime;
        private float shakeMagnitude;
        private float lastRotationTarget;
        private float currentRotationTarget;
        private float dimTime = Mathf.Infinity;
        private float growTime = Mathf.Infinity;

        // Cached References
        private readonly DamageTextSpawnerElement damageTextSpawner;
        private readonly IVisualElementScheduledItem effectsUpdate;
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

            effectsUpdate = schedule.Execute(UpdateEffects).Every(0);
            effectsUpdate.Pause();

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
            if (subscribedModel != null)
            {
                subscribedModel.damageTextQueued -= damageTextSpawner.Enqueue;
                subscribedModel.shakeRequested -= StartShake;
                subscribedModel.dimBlipRequested -= StartDim;
                subscribedModel.growBlipRequested -= StartGrow;
            }
            subscribedModel = model;
            if (subscribedModel == null) { return; }

            subscribedModel.damageTextQueued += damageTextSpawner.Enqueue;
            subscribedModel.shakeRequested += StartShake;
            subscribedModel.dimBlipRequested += StartDim;
            subscribedModel.growBlipRequested += StartGrow;
        }

        private void StartShake(bool isStrong)
        {
            shakeMagnitude = _damageShakeMagnitude * (isStrong ? _criticalDamageShakeMultiplier : 1f);
            SetShakeTarget();
            shakeTime = 0f;
            shakeStepTime = 0f;
            effectsUpdate.Resume();
        }

        private void StartDim()
        {
            dimTime = 0f;
            effectsUpdate.Resume();
        }

        private void StartGrow()
        {
            growTime = 0f;
            effectsUpdate.Resume();
        }

        private void UpdateEffects()
        {
            float deltaTime = Time.deltaTime;
            bool isShaking = UpdateShake(deltaTime);
            bool isDimming = UpdateDim(deltaTime);
            bool isGrowing = UpdateGrow(deltaTime);
            if (!isShaking && !isDimming && !isGrowing) { effectsUpdate.Pause(); }
        }

        private bool UpdateShake(float deltaTime)
        {
            if (shakeTime > _shakeDuration)
            {
                style.rotate = StyleKeyword.Null;
                return false;
            }

            const float stepDuration = _shakeDuration / _shakeCount;
            if (shakeStepTime > stepDuration)
            {
                SetShakeTarget();
                shakeStepTime = 0f;
            }
            style.rotate = new Rotate(Mathf.Lerp(lastRotationTarget, currentRotationTarget, shakeStepTime / stepDuration));

            shakeStepTime += deltaTime;
            shakeTime += deltaTime;
            return true;
        }

        private void SetShakeTarget()
        {
            shakeMagnitude = Mathf.Max(0f, shakeMagnitude - _damageShakeMagnitude / _shakeCount);
            lastRotationTarget = currentRotationTarget;
            currentRotationTarget = Random.Range(-shakeMagnitude, shakeMagnitude);
        }

        private bool UpdateDim(float deltaTime)
        {
            if (dimTime >= 2f * _halfDimmingTime)
            {
                style.opacity = StyleKeyword.Null;
                return false;
            }

            dimTime += deltaTime;
            float dimProgress = dimTime < _halfDimmingTime ? dimTime / _halfDimmingTime : 2f - dimTime / _halfDimmingTime;
            style.opacity = Mathf.Lerp(1f, _dimmingMin, Mathf.Clamp01(dimProgress));
            return true;
        }

        private bool UpdateGrow(float deltaTime)
        {
            if (growTime >= 2f * _halfGrowingTime)
            {
                style.scale = StyleKeyword.Null;
                return false;
            }

            growTime += deltaTime;
            float growProgress = growTime < _halfGrowingTime ? growTime / _halfGrowingTime : 2f - growTime / _halfGrowingTime;
            style.scale = new Scale(new Vector2(Mathf.Lerp(1f, _growingMax, Mathf.Clamp01(growProgress)), 1f)); // Horizontal only
            return true;
        }
        #endregion
    }
}
