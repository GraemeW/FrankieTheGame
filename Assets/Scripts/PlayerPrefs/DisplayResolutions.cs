using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Settings
{
    public class DisplayResolutions
    {
        private static int bestResolutionTryCount = 20;
        private static float ultraWideThreshold = 16.0f / 9.0f;

        public static List<ResolutionSetting> GetBestWindowedResolution(ResolutionSetting targetDefaultResolution, int count)
        {
            // Sanitize inputs & grab display
            count = Mathf.Clamp(count, 1, bestResolutionTryCount);
            Display display = GetCurrentDisplay();
        
            // Define display edges
            bool isShortEdgeHeight = display.systemHeight <= display.systemWidth;
            int divisor = isShortEdgeHeight ? (display.systemHeight / targetDefaultResolution.height) : (display.systemWidth / targetDefaultResolution.width);
            int shortEdge = isShortEdgeHeight ? display.systemHeight : display.systemWidth;
            int longEdge = isShortEdgeHeight ? display.systemWidth : display.systemHeight;

            // Iterate & build list
            int option = 0;
            bool traversingDown = true;
            List<ResolutionSetting> resolutionSettings = new List<ResolutionSetting>();
            for (int i = 0; i < bestResolutionTryCount; i++)
            {
                int divisorMod = divisor + option;
                if (divisorMod <= 0) { option = 1; traversingDown = false; continue; }

                int shortEdgeLength = shortEdge / divisorMod;
                if (longEdge % divisorMod == 0)
                {
                    int longEdgeLength = longEdge / divisorMod;
                    if (shortEdgeLength < shortEdge && longEdgeLength < longEdge)
                    {
                        int height = isShortEdgeHeight ? shortEdgeLength : longEdgeLength;
                        int width = isShortEdgeHeight ? longEdgeLength : shortEdgeLength;

                        resolutionSettings.Add(new ResolutionSetting(FullScreenMode.Windowed, width, height));
                    }
                    else
                    {
                        option = 0;
                        traversingDown = false;
                    }
                }

                if (traversingDown) { option--; }
                else { option++; }
                if (resolutionSettings.Count >= count) { break; }
            }

            if (resolutionSettings.Count == 0) { resolutionSettings.Add(new ResolutionSetting(FullScreenMode.Windowed, display.systemWidth, display.systemHeight)); }

            return resolutionSettings;
        }

        public static ResolutionSetting GetFSWResolution()
        {
            Display display = GetCurrentDisplay();

            ResolutionSetting resolutionSetting = new ResolutionSetting(FullScreenMode.FullScreenWindow, display.systemWidth, display.systemHeight);
            if (((float)display.systemWidth / (float)display.systemHeight) > (ultraWideThreshold + Mathf.Epsilon))
            {
                resolutionSetting.height = Mathf.RoundToInt(display.systemHeight * ultraWideThreshold);
            }
            return resolutionSetting;
        }

        private static Display GetCurrentDisplay()
        {
            Display display = Display.main;

            int currentDisplayIndex = GetCurrentDisplayNumber();
            if (currentDisplayIndex < Display.displays.Length)
            {
                display = Display.displays[currentDisplayIndex];
            }
            return display;
        }

        private static int GetCurrentDisplayNumber()
        {
            List<DisplayInfo> displayLayout = new List<DisplayInfo>();
            Screen.GetDisplayLayout(displayLayout);
            return displayLayout.IndexOf(Screen.mainWindowDisplayInfo);
        }
    }
}
