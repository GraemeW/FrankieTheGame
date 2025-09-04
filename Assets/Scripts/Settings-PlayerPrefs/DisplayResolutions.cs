using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Settings
{
    public class DisplayResolutions
    {
        private static ResolutionSetting targetDefaultResolution = new ResolutionSetting(FullScreenMode.Windowed, 800, 600);
        private static int bestResolutionTryCount = 20;
        private static float ultraWideThreshold = 16.0f / 9.0f;

        public static ResolutionSetting GetCurrentResolution()
        {
            ResolutionSetting resolutionSetting = new ResolutionSetting(Screen.fullScreenMode, Screen.width, Screen.height);
            return resolutionSetting;
        }

        public static List<ResolutionSetting> GetBestWindowedResolution(int count)
        {
            // Sanitize inputs & grab display
            count = Mathf.Clamp(count, 1, bestResolutionTryCount);
            DisplayInfo displayInfo = Screen.mainWindowDisplayInfo;

            // Define display edges
            bool isShortEdgeHeight = displayInfo.height <= displayInfo.width;
            int divisor = isShortEdgeHeight ? (displayInfo.height / targetDefaultResolution.height) : (displayInfo.width / targetDefaultResolution.width);
            int shortEdge = isShortEdgeHeight ? displayInfo.height : displayInfo.width;
            int longEdge = isShortEdgeHeight ? displayInfo.width : displayInfo.height;

            // Iterate & build list
            int option = divisor;
            bool traversingDown = true;
            List<ResolutionSetting> resolutionSettings = new List<ResolutionSetting>();
            for (int i = 0; i < bestResolutionTryCount; i++)
            {
                if (option == 0) { option = divisor + 1; traversingDown = false; continue; }

                int shortEdgeLength = shortEdge / option;
                if (longEdge % option == 0)
                {
                    int longEdgeLength = longEdge / option;
                    if (!isShortEdgeHeight)
                    {
                        float longEdgeTarget = (float)shortEdgeLength * (float)shortEdge / (float)longEdge;
                        int longEdgeDivisor = Mathf.CeilToInt((float)longEdge / longEdgeTarget);
                        longEdgeLength = longEdge / longEdgeDivisor;
                    }

                    if (shortEdgeLength > shortEdge || longEdgeLength > longEdge // invalid entry
                        || (shortEdgeLength == shortEdge && longEdgeLength == longEdge)) // use FSW
                    {
                        if (traversingDown) { option--; } else { option++; }
                        continue;
                    }

                    int width = isShortEdgeHeight ? longEdgeLength : shortEdgeLength;
                    int height = isShortEdgeHeight ? shortEdgeLength : longEdgeLength;
                    resolutionSettings.Add(new ResolutionSetting(FullScreenMode.Windowed, width, height));
                }

                if (traversingDown) { option--; } else { option++; }
                if (resolutionSettings.Count >= count) { break; }
            }

            if (resolutionSettings.Count == 0) { resolutionSettings.Add(new ResolutionSetting(FullScreenMode.Windowed, displayInfo.width, displayInfo.height)); }

            return resolutionSettings;
        }

        public static ResolutionSetting GetFSWResolution()
        {
            DisplayInfo displayInfo = Screen.mainWindowDisplayInfo;
            ResolutionSetting resolutionSetting = new ResolutionSetting(FullScreenMode.FullScreenWindow, displayInfo.width, displayInfo.height);

            if (((float)displayInfo.width / (float)displayInfo.height) > (ultraWideThreshold + Mathf.Epsilon))
            {
                resolutionSetting.height = Mathf.RoundToInt(displayInfo.height * ultraWideThreshold);
            }
            return resolutionSetting;
        }

        public static IEnumerator UpdateScreenResolution(ResolutionSetting resolutionSetting)
        {
            UnityEngine.Debug.Log($"Resolution is updating to {resolutionSetting.width} x {resolutionSetting.height} on FSW: {resolutionSetting.fullScreenMode}");

            Screen.fullScreenMode = resolutionSetting.fullScreenMode;
            yield return new WaitForEndOfFrame();

            Screen.SetResolution(resolutionSetting.width, resolutionSetting.height, resolutionSetting.fullScreenMode);
            yield return new WaitForEndOfFrame();
        }

        public static void SetWindowToCenter()
        {
            DisplayInfo displayInfo = Screen.mainWindowDisplayInfo;
            Vector2Int position = new Vector2Int((displayInfo.width - Screen.width) / 2, (displayInfo.height - Screen.height) / 2);

            Screen.MoveMainWindowTo(displayInfo, position);
        }
    }
}
