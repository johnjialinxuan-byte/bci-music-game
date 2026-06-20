#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace MusicGame.EditorTools
{
    /// <summary>
    /// Injects iOS-only Info.plist keys after each build so they survive Unity
    /// regenerating the Xcode project (manual Xcode edits get wiped on Replace builds).
    /// iOS-only by both the #if guard and the BuildTarget check, so it never affects
    /// Windows/macOS builds.
    /// </summary>
    public static class IOSBuildPostProcessor
    {
        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            string plistPath = pathToBuiltProject + "/Info.plist";
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            PlistElementDict root = plist.root;

            // Allow >60Hz (ProMotion 120Hz) on iPhone/iPad Pro. Without this iOS
            // clamps the frame rate to 60 even when targetFrameRate is higher.
            root.SetBoolean("CADisableMinimumFrameDurationOnPhone", true);

            // Declare the app as a game so iOS 18+/macOS Game Mode can apply.
            root.SetBoolean("LSSupportsGameMode", true);

            plist.WriteToFile(plistPath);
        }
    }
}
#endif
