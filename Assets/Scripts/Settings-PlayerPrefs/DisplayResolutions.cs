using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Settings
{
    public class DisplayResolutions
    {
        // Default Static Parameters
        private static ResolutionSetting targetDefaultResolution = new ResolutionSetting(FullScreenMode.Windowed, 848, 477);
        private static int bestResolutionTryCount = 20;
        private static float ultraWideThreshold = 16.0f / 9.0f;
        private static ResolutionScaler standardWindowedResolutionScaler = new ResolutionScaler(4, 3, 2);
        private static int[] extraZoomThresholds = new int[] { 960, 540 };

        // Events
        public static event Action<ResolutionScaler> resolutionUpdated;

        public static ResolutionSetting GetCurrentResolution()
        {
            ResolutionSetting resolutionSetting = new ResolutionSetting(Screen.fullScreenMode, Screen.width, Screen.height);
            return resolutionSetting;
        }

        public static float[] GetPixelsPerTexel()
        {
            float[] pixelsPerTexel = new float[] { 1.0f, 1.0f };
            pixelsPerTexel[0] = (float)Screen.width / (float)targetDefaultResolution.width;
            pixelsPerTexel[1] = (float)Screen.height / (float)targetDefaultResolution.height;
            return pixelsPerTexel;
        }

        public static ResolutionScaler GetResolutionScaler()
        {
            if (Screen.fullScreenMode != FullScreenMode.Windowed) { return new ResolutionScaler(1, 1, 1); }

            ResolutionScaler resolutionScaler = new ResolutionScaler(standardWindowedResolutionScaler);
            if (Screen.width < extraZoomThresholds[0] || Screen.height < extraZoomThresholds[1]) { resolutionScaler.cameraScaling *= 2; }
            return resolutionScaler;
        }

        public static List<ResolutionSetting> GetBestWindowedResolution(int count, bool ignoreTargetResolution = true)
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
            if (ignoreTargetResolution) { option = 1; traversingDown = false; }

            List<ResolutionSetting> resolutionSettings = new List<ResolutionSetting>();
            for (int i = 0; i < bestResolutionTryCount; i++)
            {
                if (option == 0) { option = divisor + 1; traversingDown = false; continue; }

                int shortEdgeLength = shortEdge / option;
                if (longEdge % option == 0)
                {
                    int longEdgeLength = longEdge / option;
                    int width = longEdgeLength * standardWindowedResolutionScaler.numerator / standardWindowedResolutionScaler.denominator;
                    int height = shortEdgeLength * standardWindowedResolutionScaler.numerator / standardWindowedResolutionScaler.denominator;

                    if (width > displayInfo.width || height > displayInfo.height || (width == displayInfo.width && height == displayInfo.height))
                    {
                        if (traversingDown) { option--; } else { option++; }
                        continue;
                    }

                    if (width < targetDefaultResolution.width || height < targetDefaultResolution.height) { break; }
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

            bool isFullScreen = !(resolutionSetting.fullScreenMode == FullScreenMode.Windowed);

            resolutionUpdated?.Invoke(GetResolutionScaler());
        }

        public static void SetWindowToCenter()
        {
            DisplayInfo displayInfo = Screen.mainWindowDisplayInfo;
            Vector2Int position = new Vector2Int((displayInfo.width - Screen.width) / 2, (displayInfo.height - Screen.height) / 2);

            Screen.MoveMainWindowTo(displayInfo, position);
        }
    }
}
