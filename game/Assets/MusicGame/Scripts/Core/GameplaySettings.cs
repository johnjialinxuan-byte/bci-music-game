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
        private const string AttentionDefaultsVersionKey = "Gameplay.Attention.DefaultsVersion";
        private const string ChartDelayKey = "Gameplay.Chart.DelayMs";
        private const string ChartSpeedTenthsKey = "Gameplay.Chart.SpeedTenths";

        public static int EasyAttention
        {
            get => Read(EasyAttentionKey, 10, 0, 100);
            set => Write(EasyAttentionKey, value, 0, 100);
        }

        public static int NormalAttention
        {
            get => Read(NormalAttentionKey, 10, 0, 100);
            set => Write(NormalAttentionKey, value, 0, 100);
        }

        public static int HardAttention
        {
            get => Read(HardAttentionKey, 20, 0, 100);
            set => Write(HardAttentionKey, value, 0, 100);
        }

        public static int FlickPerfectMs
        {
            get => Read(FlickPerfectKey, 50, 40, 120);
            set => Write(FlickPerfectKey, value, 40, 120);
        }

        public static int FlickGreatMs
        {
            // BCI gyro packets arrive at ~10Hz, so detection alone can lag ~100ms;
            // the GREAT window must absorb that or head flicks can never land.
            get => Read(FlickGreatKey, 250, 120, 500);
            set => Write(FlickGreatKey, value, 120, 500);
        }

        public static float FlickPerfectWindow => FlickPerfectMs / 1000f;
        public static float FlickGreatWindow => FlickGreatMs / 1000f;

        public static int ChartDelayMs
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(ChartDelayKey, 0), -400, 400);
            set
            {
                PlayerPrefs.SetInt(ChartDelayKey, Mathf.Clamp(value, -400, 400));
                PlayerPrefs.Save();
            }
        }

        public static float ChartSpeed
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(ChartSpeedTenthsKey, 20) / 10f, 1f, 5f);
            set
            {
                int tenths = Mathf.Clamp(Mathf.RoundToInt(value * 10f), 10, 50);
                PlayerPrefs.SetInt(ChartSpeedTenthsKey, tenths);
                PlayerPrefs.Save();
            }
        }

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

        public static void InitializeAttentionDefaults()
        {
            if (PlayerPrefs.GetInt(AttentionDefaultsVersionKey, 0) >= 1) return;

            bool hasExistingValues = PlayerPrefs.HasKey(EasyAttentionKey)
                || PlayerPrefs.HasKey(NormalAttentionKey)
                || PlayerPrefs.HasKey(HardAttentionKey);
            bool isPreviousDefault = EasyAttention == 50
                && NormalAttention == 50
                && HardAttention == 50;

            if (!hasExistingValues || isPreviousDefault)
            {
                EasyAttention = 10;
                NormalAttention = 10;
                HardAttention = 20;
            }

            PlayerPrefs.SetInt(AttentionDefaultsVersionKey, 1);
            PlayerPrefs.Save();
        }
    }
}
