using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Utils.Editor
{
    public class StandStillAnimationData
    {
        private readonly string prefix;
        private readonly string characterName;
        private readonly string outputPath;
        private readonly Dictionary<string, Sprite> sourceSprites;
        private readonly Sprite downFirstFrame;

        public StandStillAnimationData(string prefix, string characterName, Dictionary<string, Sprite> sourceSprites, Sprite downFirstFrame, string outputPath)
        {
            this.prefix = prefix;
            this.characterName = characterName;
            this.sourceSprites = sourceSprites;
            this.downFirstFrame = downFirstFrame;
            this.outputPath = outputPath;
        }

        public string GetClipName() => $"{prefix}{characterName}StandStill";
        public string GetClipAssetPath() => $"{outputPath}/{GetClipName()}.anim";
        public Sprite[] GetStandStillSprites()
        {
            Sprite standStillSprite = null;
            if (sourceSprites.TryGetValue(characterName, out Sprite dedicated) && dedicated != null)
            {
                standStillSprite = dedicated;
            }
            else if (downFirstFrame != null)
            {
                standStillSprite = downFirstFrame;
            }
            Sprite[] sprites = standStillSprite != null ? new[] { standStillSprite } : Array.Empty<Sprite>();
            return sprites;
        }
    }
}
