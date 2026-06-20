using UnityEngine;

namespace MusicGame
{
    /// <summary>
    /// Unlocks high refresh rate (ProMotion 120Hz). iOS defaults to 30fps unless
    /// Application.targetFrameRate is raised. The actual >60Hz ProMotion cap also
    /// needs the Info.plist key CADisableMinimumFrameDurationOnPhone, which is added
    /// by IOSBuildPostProcessor.
    ///
    /// Desktop is left untouched: on Windows/macOS vSync stays on, so targetFrameRate
    /// is effectively ignored there and the game still runs at the monitor refresh.
    /// </summary>
    public static class FrameRateController
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // High target everywhere; harmless on desktop (vSync clamps it there).
            Application.targetFrameRate = 120;

            // Only disable vSync on mobile so targetFrameRate is honored. Leaving
            // desktop vSync alone keeps Windows/macOS behavior exactly as before.
            if (Application.isMobilePlatform)
                QualitySettings.vSyncCount = 0;
        }
    }
}
