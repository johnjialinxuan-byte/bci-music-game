namespace MusicGame.Audio
{
    public static class MusicTime
    {
        public static float Current => AudioManager.Instance != null
            ? AudioManager.Instance.GetCurrentTime()
            : 0f;

        public static float CurrentMilliseconds => Current * 1000f;

        public static bool IsPlaying => AudioManager.Instance != null && AudioManager.Instance.IsPlaying();
    }
}
