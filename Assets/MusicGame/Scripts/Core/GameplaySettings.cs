using UnityEngine;

namespace MusicGame.Core
{
    public static class GameplaySettings
    {
        private const int Step = 5;
        private const string EasyAttentionKey = "Gameplay.Attention.Easy";
        private const string NormalAttentionKey = "Gameplay.Attention.Normal";
        private const string HardAttentionKey = "Gameplay.Attention.Hard";
        private const string FlickPerfectKey = "Gameplay.Flick.PerfectMs";
        private const string FlickGreatKey = "Gameplay.Flick.GreatMs";

        public static int EasyAttention
        {
            get => Read(EasyAttentionKey, 50, 0, 100);
            set => Write(EasyAttentionKey, value, 0, 100);
        }

        public static int NormalAttention
        {
            get => Read(NormalAttentionKey, 50, 0, 100);
            set => Write(NormalAttentionKey, value, 0, 100);
        }

        public static int HardAttention
        {
            get => Read(HardAttentionKey, 50, 0, 100);
            set => Write(HardAttentionKey, value, 0, 100);
        }

        public static int FlickPerfectMs
        {
            get => Read(FlickPerfectKey, 50, 40, 120);
            set => Write(FlickPerfectKey, value, 40, 120);
        }

        public static int FlickGreatMs
        {
            get => Read(FlickGreatKey, 120, 120, 200);
            set => Write(FlickGreatKey, value, 120, 200);
        }

        public static float FlickPerfectWindow => FlickPerfectMs / 1000f;
        public static float FlickGreatWindow => FlickGreatMs / 1000f;

        public static int GetAttentionThreshold(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Easy => EasyAttention,
                Difficulty.Hard => HardAttention,
                _ => NormalAttention
            };
        }

        private static int Read(string key, int defaultValue, int min, int max)
        {
            return Snap(PlayerPrefs.GetInt(key, defaultValue), min, max);
        }

        private static void Write(string key, int value, int min, int max)
        {
            PlayerPrefs.SetInt(key, Snap(value, min, max));
            PlayerPrefs.Save();
        }

        private static int Snap(int value, int min, int max)
        {
            return Mathf.Clamp(Mathf.RoundToInt(value / (float)Step) * Step, min, max);
        }
    }
}
