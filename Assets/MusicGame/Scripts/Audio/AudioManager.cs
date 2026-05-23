using UnityEngine;
using CriWare;

namespace MusicGame.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private string cueSheetName = "";
        [SerializeField] private string cueName = "";

        private CriAtomExPlayer player;
        private CriAtomExPlayback playback;
        private bool playing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            player?.Dispose();
            player = null;
        }

        public void PlaySong()
        {
            PlaySong(cueSheetName, cueName);
        }

        public void PlaySong(string cueSheet, string cue)
        {
            cueSheetName = cueSheet;
            cueName = cue;

            if (player == null)
            {
                player = new CriAtomExPlayer(true);
            }

            CriAtomExAcb acb = CriAtom.GetAcb(cueSheetName);
            if (acb == null)
            {
                Debug.LogError($"[AudioManager] CRIWARE ACB not found: {cueSheetName}");
                playing = false;
                return;
            }

            player.SetCue(acb, cueName);
            playback = player.Start();
            playing = true;
        }

        public void StopSong()
        {
            player?.Stop();
            playing = false;
        }

        public void Pause()
        {
            if (player == null || !playing) return;
            player.Pause();
        }

        public void Resume()
        {
            if (player == null || !playing) return;
            player.Resume(CriAtomEx.ResumeMode.AllPlayback);
        }


        public float GetCurrentTime()
        {
            if (!playing || playback.id == CriAtomExPlayback.invalidId)
            {
                return 0f;
            }

            long timeMs = playback.GetTimeSyncedWithAudio();
            if (timeMs < 0)
            {
                timeMs = playback.GetTime();
            }

            return timeMs < 0 ? 0f : timeMs / 1000f;
        }

        public bool IsPlaying()
        {
            return playing;
        }
    }
}
