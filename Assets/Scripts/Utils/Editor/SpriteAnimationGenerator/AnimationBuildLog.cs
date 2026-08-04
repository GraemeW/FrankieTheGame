using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Frankie.Utils.Editor
{
    public class AnimationBuildLog
    {
        public readonly StringBuilder log = new();
        public int createdCount = 0;
        private int skippedCount = 0;

        public void AppendLine(string line = "")
        {
            log.AppendLine(line);
        }

        public void SummarizeGeneration(string line = "")
        {
            log.Insert(0, $"Done. {createdCount} clip(s) written, {skippedCount} skipped.\n\n");
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
        }

        public void SkipNoSprite(string clipName)
        {
            log.AppendLine($"Skipped {clipName}: no loadable sprites found.");
            skippedCount++;
        }

        public void SkipAlreadyExists(string clipName)
        {
            log.AppendLine($"Skipped {clipName}: already exists (overwrite disabled).");
            skippedCount++;
        }
    }
}
