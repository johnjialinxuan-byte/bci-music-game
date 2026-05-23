namespace MusicGame.Audio
{
    public static class MusicTime
    {
        public static float Current
        {
            get
            {
                if (AudioManager.Instance != null && AudioManager.Instance.IsPlaying())
                {
                    return AudioManager.Instance.GetCurrentTime();
                }

                if (CriAudioManager.Instance != null && CriAudioManager.Instance.IsPlaying)
                {
                    return CriAudioManager.Instance.GetCurrentTime();
                }

                return 0f;
            }
        }
    }
}
