using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Combat.UI
{
    public sealed class DamageTextElement : Label
    {
        // Const Tunables
        private const string _ussClassName = "damage-text";
        private const float _duration = 0.7f;
        private const float _peakTime = 0.42f;
        private const float _minScale = 0.035f;
        private const float _rise = 50f;

        // State
        private float elapsedTime = 0f;

        // Cached References
        private readonly IVisualElementScheduledItem animationUpdate;

        public DamageTextElement(string text, string modifierUssClassName) : base(text)
        {
            AddToClassList(_ussClassName);
            if (modifierUssClassName != null) { AddToClassList(modifierUssClassName); }
            pickingMode = PickingMode.Ignore;
            ApplyAnimation(0f);
            animationUpdate = schedule.Execute(UpdateAnimation).Every(0);
        }

        #region PrivateMethods
        private void UpdateAnimation()
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= _duration)
            {
                animationUpdate.Pause();
                RemoveFromHierarchy();
                return;
            }
            ApplyAnimation(elapsedTime);
        }

        private void ApplyAnimation(float time)
        {
            float scale = time < _peakTime
                ? Mathf.Lerp(_minScale, 1f, time / _peakTime)
                : Mathf.Lerp(1f, _minScale, (time - _peakTime) / (_duration - _peakTime));
            float opacity = time < _peakTime ? 1f : Mathf.Lerp(1f, 0f, (time - _peakTime) / (_duration - _peakTime));

            style.scale = new Scale(new Vector2(scale, scale));
            style.translate = new Translate(0f, -_rise * time / _duration);
            style.opacity = opacity;
        }
        #endregion
    }
}
