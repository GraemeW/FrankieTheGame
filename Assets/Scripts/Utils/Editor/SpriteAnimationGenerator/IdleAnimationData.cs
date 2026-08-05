using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Utils.Editor
{
    public class IdleAnimationData
    {
        private readonly string prefix;
        private readonly string characterName;
        private readonly string action;
        private readonly Sprite[] movementSprites;
        private readonly Dictionary<(string character, string action), Sprite[]> idleSourceSprites;
        private readonly string outputPath;

        public IdleAnimationData(string prefix, string characterName, string action, Sprite[] movementSprites, Dictionary<(string character, string action), Sprite[]> idleSourceSprites, string outputPath)
        {
            this.prefix = prefix;
            this.characterName = characterName;
            this.action = action;
            this.movementSprites = movementSprites;
            this.idleSourceSprites = idleSourceSprites;
            this.outputPath = outputPath;
        }
        
        public string GetClipName() => $"{prefix}{characterName}{SpriteAnimationGeneratorWindow.idleOverrideToken}{action}";
        public string GetClipAssetPath() => $"{outputPath}/{GetClipName()}.anim";

        public Sprite[] GetIdleSprites()
        {
            Sprite[] idleSprites;
            if (idleSourceSprites.TryGetValue((characterName, action), out Sprite[] dedicated) && dedicated.Length > 0)
            {
                idleSprites = dedicated;
            }
            else if (string.Equals(action, SpriteAnimationGeneratorWindow.standardDownToken, StringComparison.OrdinalIgnoreCase))
            {
                idleSprites = movementSprites;
            }
            else
            {
                idleSprites = new[] { movementSprites[0] };
            }
            return idleSprites;
        }
    }
}
