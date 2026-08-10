using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UIElements;

namespace LowDefMustard.Utils.Editor
{
    public class AnimationBuildLog
    {
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
            log.Insert(0, $"Done. {createdCount} clip(s) written, {skippedCount} skipped.\n\n");
            Publish();
        }

        public void AnnotatePassthroughActions(HashSet<string> passthroughActions)
        {
            if (passthroughActions.Count <= 0) { return; }
            log.AppendLine();
            log.AppendLine("Non-standard actions used as-is (not in DirectionAliasMap):");
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
            log.AppendLine("Ambiguous actions identified in OverrideController:");
            foreach (string action in ambiguousActions.OrderBy(s => s))
            {
                log.AppendLine($"  {action}");
            }
            Publish();
        }

        public void SkipNoSprite(string clipName)
        {
            log.AppendLine($"Skipped {clipName}: no loadable sprites found.");
            skippedCount++;
            Publish();
        }

        public void SkipAlreadyExists(string clipName)
        {
            log.AppendLine($"Skipped {clipName}: already exists (overwrite disabled).");
            skippedCount++;
            Publish();
        }
    }
}
