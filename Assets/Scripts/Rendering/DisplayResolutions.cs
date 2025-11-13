using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Rendering
{
    public static class DisplayResolutions
    {
        // Default Static Parameters
        private static readonly ResolutionSetting _targetDefaultResolution = new(FullScreenMode.Windowed, 848, 477);
        private const int _bestResolutionTryCount = 20;
        private const float _ultraWideThreshold = 16.0f / 9.0f;
        private static readonly ResolutionScaler _windowedResolutionScaler = new(4, 3);
        private static readonly int[] _fineZoomThresholds = { 960, 540 }; // width, height

        // State
        private static FullScreenMode _currentFullScreenMode = FullScreenMode.Windowed;
        private static int _currentScreenWidth = 848;
        private static int _currentScreenHeight = 477;

        // Events
        public static event Action<ResolutionScaler, int> resolutionUpdated;

        #region PrivateMethods
        private static ResolutionScaler GetResolutionScaler()
        {
            if (Screen.fullScreenMode != FullScreenMode.Windowed) { return new ResolutionScaler(1, 1); }
            return _windowedResolutionScaler;
        }

        private static int GetCameraScaling()
        {
            int cameraScaling = 1;
            if (Screen.fullScreenMode == FullScreenMode.Windowed) { cameraScaling *= 2; }
            if (Screen.width < _fineZoomThresholds[0] || Screen.height < _fineZoomThresholds[1]) { cameraScaling *= 2; }
#if UNITY_EDITOR
            cameraScaling = 2; // Disable excessive camera scaling for editor window
#endif

            return cameraScaling;
        }
        #endregion

        #region PublicMethods
        public static void CheckForResolutionChange()
        {
            if (_currentFullScreenMode != Screen.fullScreenMode || _currentScreenWidth != Screen.width || _currentScreenHeight != Screen.height)
            {
                _currentFullScreenMode = Screen.fullScreenMode;
                _currentScreenWidth = Screen.width;
                _currentScreenHeight = Screen.height;

                resolutionUpdated?.Invoke(GetResolutionScaler(), GetCameraScaling());
            }
        }

        public static void AnnounceResolution()
        {
            resolutionUpdated?.Invoke(GetResolutionScaler(), GetCameraScaling());
        }

        public static ResolutionSetting GetCurrentResolution()
        {
            ResolutionSetting resolutionSetting = new ResolutionSetting(Screen.fullScreenMode, Screen.width, Screen.height);
            return resolutionSetting;
        }

        public static List<ResolutionSetting> GetBestWindowedResolution(int count, bool ignoreTargetResolution = true)
        {
            // Sanitize inputs & grab display
            count = Mathf.Clamp(count, 1, _bestResolutionTryCount);
            DisplayInfo displayInfo = Screen.mainWindowDisplayInfo;

            // Define display edges
            bool isShortEdgeHeight = displayInfo.height <= displayInfo.width;
            int divisor = isShortEdgeHeight ? (displayInfo.height / _targetDefaultResolution.height) : (displayInfo.width / _targetDefaultResolution.width);
            int shortEdge = isShortEdgeHeight ? displayInfo.height : displayInfo.width;
            int longEdge = isShortEdgeHeight ? displayInfo.width : displayInfo.height;

            // Iterate & build list
            int option = divisor;
            bool traversingDown = true;
            if (ignoreTargetResolution) { option = 1; traversingDown = false; }

            List<ResolutionSetting> resolutionSettings = new List<ResolutionSetting>();
            for (int i = 0; i < _bestResolutionTryCount; i++)
            {
                if (option == 0) { option = divisor + 1; traversingDown = false; continue; }

                int shortEdgeLength = shortEdge / option;
                if (longEdge % option == 0)
                {
                    int longEdgeLength = longEdge / option;
                    int width = longEdgeLength * _windowedResolutionScaler.numerator / _windowedResolutionScaler.denominator;
                    int height = shortEdgeLength * _windowedResolutionScaler.numerator / _windowedResolutionScaler.denominator;

                    if (width > displayInfo.width || height > displayInfo.height || (width == displayInfo.width && height == displayInfo.height))
                    {
                        if (traversingDown) { option--; } else { option++; }
                        continue;
                    }

                    if (width < _targetDefaultResolution.width || height < _targetDefaultResolution.height) { break; }
                    resolutionSettings.Add(new ResolutionSetting(FullScreenMode.Windowed, width, height));
                }

                if (traversingDown) { option--; } else { option++; }
                if (resolutionSettings.Count >= count) { break; }
            }

            if (resolutionSettings.Count == 0) { resolutionSettings.Add(new ResolutionSetting(FullScreenMode.Windowed, displayInfo.width, displayInfo.height)); }

            return resolutionSettings;
        }

        public static ResolutionSetting GetFullScreenWidthResolution()
        {
            DisplayInfo displayInfo = Screen.mainWindowDisplayInfo;
            ResolutionSetting resolutionSetting = new ResolutionSetting(FullScreenMode.FullScreenWindow, displayInfo.width, displayInfo.height);

            if (((float)displayInfo.width / displayInfo.height) > (_ultraWideThreshold + Mathf.Epsilon))
            {
                resolutionSetting.height = Mathf.RoundToInt(displayInfo.height * _ultraWideThreshold);
            }
            return resolutionSetting;
        }

        public static IEnumerator UpdateScreenResolution(ResolutionSetting resolutionSetting)
        {
            Debug.Log($"Resolution is updating to {resolutionSetting.width} x {resolutionSetting.height} on FSW: {resolutionSetting.fullScreenMode}");

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
        #endregion
    }
}
