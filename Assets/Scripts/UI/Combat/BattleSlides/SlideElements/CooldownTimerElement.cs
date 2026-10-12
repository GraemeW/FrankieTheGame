using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    public sealed class CooldownTimerElement : VisualElement
    {
        // Const Tunables
        private const string _ussClassName = "cooldown-timer";
        private static readonly CustomStyleProperty<Color> _foregroundColourProperty = new("--cooldown-foreground-colour");
        private static readonly CustomStyleProperty<Color> _backgroundColourProperty = new("--cooldown-background-colour");
        private static readonly CustomStyleProperty<Color> _pausedColourProperty = new("--cooldown-paused-colour");
        private static readonly CustomStyleProperty<float> _foregroundScaleProperty = new("--cooldown-foreground-scale"); // Foreground disc radius, relative to the backing disc

        // State
        private CooldownTiming internalCooldown;
        private Color foregroundColour;
        private Color backgroundColour;
        private Color pausedColour;
        private float foregroundScale = 1f;
        private float drawnFraction = -1f;

        // Cached References
        private readonly IVisualElementScheduledItem fillUpdate;

        [CreateProperty] public CooldownTiming cooldown
        {
            get => internalCooldown;
            set
            {
                internalCooldown = value;
                fillUpdate.Resume();
                MarkDirtyRepaint();
            }
        }

        public CooldownTimerElement(string sourcePropertyName)
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += DrawTimer;
            RegisterCallback<CustomStyleResolvedEvent>(HandleCustomStyleResolved);
            fillUpdate = schedule.Execute(UpdateFill).Every(0);
            SetBinding(nameof(cooldown), UIToolkitBindings.ToTarget(sourcePropertyName));
        }

        #region PrivateMethods
        private void HandleCustomStyleResolved(CustomStyleResolvedEvent customStyleResolvedEvent)
        {
            ICustomStyle newStyle = customStyleResolvedEvent.customStyle;
            if (newStyle.TryGetValue(_foregroundColourProperty, out Color resolvedForeground)) { foregroundColour = resolvedForeground; }
            if (newStyle.TryGetValue(_backgroundColourProperty, out Color resolvedBackground)) { backgroundColour = resolvedBackground; }
            if (newStyle.TryGetValue(_pausedColourProperty, out Color resolvedPaused)) { pausedColour = resolvedPaused; }
            if (newStyle.TryGetValue(_foregroundScaleProperty, out float resolvedForegroundScale)) { foregroundScale = resolvedForegroundScale; }
            MarkDirtyRepaint();
        }

        private void UpdateFill()
        {
            float fraction = internalCooldown.GetFraction(Time.time);
            if (!Mathf.Approximately(fraction, drawnFraction)) { MarkDirtyRepaint(); }
            if (fraction >= 1f) { fillUpdate.Pause(); }
        }

        private void DrawTimer(MeshGenerationContext meshGenerationContext)
        {
            Rect bounds = contentRect;
            if (bounds.width <= 0f || bounds.height <= 0f) { return; }

            drawnFraction = internalCooldown.GetFraction(Time.time);
            Vector2 centre = bounds.center;
            float radius = Mathf.Min(bounds.width, bounds.height) * 0.5f;
            Painter2D painter = meshGenerationContext.painter2D;

            painter.fillColor = internalCooldown.isPaused ? pausedColour : backgroundColour;
            painter.BeginPath();
            painter.Arc(centre, radius, 0f, 360f);
            painter.Fill();

            if (drawnFraction <= 0f) { return; }
            painter.fillColor = foregroundColour;
            painter.BeginPath();
            if (drawnFraction >= 1f)
            {
                painter.Arc(centre, radius * foregroundScale, 0f, 360f);
            }
            else
            {
                painter.MoveTo(centre);
                painter.Arc(centre, radius * foregroundScale, -90f, -90f + 360f * drawnFraction);
                painter.ClosePath();
            }
            painter.Fill();
        }
        #endregion
    }
}
