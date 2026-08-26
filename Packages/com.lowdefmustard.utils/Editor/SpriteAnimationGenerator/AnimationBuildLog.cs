using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UIElements;

namespace LowDefMustard.Utils.Editor
{
    public class AnimationBuildLog
    {
        // Tunables - marked internal for test access
        internal const string summarizeMessage = "Done. {0} clip(s) written, {1} skipped.\n \n";
        private const string _passThroughActionMessage = "Non-standard actions used as-is (not in DirectionAliasMap):";
        private const string _ambiguousActionMessage = "Ambiguous actions identified in OverrideController:";
        internal const string skipNoSpriteMessage = "Skipped {0}: no loadable sprites found.";
        internal const string skipAlreadyExistsMessage = "Skipped {0}: already exists (overwrite disabled).";

        // State
        private readonly StringBuilder log = new();
        public int createdCount = 0;
        private int skippedCount = 0;
        private readonly Label logLabel;

        public AnimationBuildLog(Label logLabel = null)
        {
            this.logLabel = logLabel;
            Publish();
        }

        private void Publish()
        {
            if (logLabel != null) { logLabel.text = log.ToString(); }
        }

        public void AppendLine(string line = "")
        {
            log.AppendLine(line);
            Publish();
        }

        public void SummarizeGeneration()
        {
            log.Insert(0, string.Format(summarizeMessage, createdCount, skippedCount));
            Publish();
        }

        public void AnnotatePassthroughActions(HashSet<string> passthroughActions)
        {
            if (passthroughActions.Count <= 0) { return; }
            log.AppendLine();
            log.AppendLine(_passThroughActionMessage);
            foreach (string action in passthroughActions.OrderBy(s => s))
            {
                log.AppendLine($"  {action}");
            }
            Publish();
        }

        public void AnnotateAmbiguousActions(List<string> ambiguousActions)
        {
            if (ambiguousActions.Count <= 0) { return; }
            log.AppendLine();
            log.AppendLine(_ambiguousActionMessage);
            foreach (string action in ambiguousActions.OrderBy(s => s))
            {
                log.AppendLine($"  {action}");
            }
            Publish();
        }

        public void SkipNoSprite(string clipName)
        {
            log.AppendLine(string.Format(skipNoSpriteMessage, clipName));
            skippedCount++;
            Publish();
        }

        public void SkipAlreadyExists(string clipName)
        {
            log.AppendLine(string.Format(skipAlreadyExistsMessage, clipName));
            skippedCount++;
            Publish();
        }
    }
}
