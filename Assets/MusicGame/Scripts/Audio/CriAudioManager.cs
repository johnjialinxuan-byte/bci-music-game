using UnityEngine;
using CriWare;

namespace MusicGame.Audio
{
    public class CriAudioManager : MonoBehaviour
    {
        public static CriAudioManager Instance { get; private set; }

        [SerializeField] private string cueSheetName = "";
        [SerializeField] private string cueName = "";

        private CriAtomExPlayer player;
        private CriAtomExPlayback currentPlayback;
        private bool isPlaying = false;
        private bool isPaused = false;

        public bool IsPlaying => isPlaying;
        public bool IsPaused => isPaused;

        public float CurrentTimeMs
        {
            get
            {
                if (!isPlaying || currentPlayback.id == CriAtomExPlayback.invalidId)
                    return 0f;
                long time = currentPlayback.GetTimeSyncedWithAudio();
                if (time < 0)
                    time = currentPlayback.GetTime();
                return time < 0 ? 0f : time;
            }
        }

        public float CurrentTimeSec => CurrentTimeMs / 1000f;

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
            if (player != null)
            {
                player.Dispose();
                player = null;
            }
        }

        public void InitializePlayer(bool audioSyncedTimer = true)
        {
            if (player != null)
            {
                player.Dispose();
            }
            player = new CriAtomExPlayer(audioSyncedTimer);
        }

        public void SetCue(string cueSheet, string cue)
        {
            cueSheetName = cueSheet;
            cueName = cue;
        }

        public void Play()
        {
            if (player == null)
            {
                InitializePlayer();
            }

            CriAtomExAcb acb = CriAtom.GetAcb(cueSheetName);
            if (acb == null)
            {
                Debug.LogError($"[CriAudioManager] ACB not found: {cueSheetName}");
                return;
            }

            player.SetCue(acb, cueName);
            currentPlayback = player.Start();
            isPlaying = true;
            isPaused = false;
        }

        public void Stop()
        {
            if (player != null)
            {
                player.Stop();
            }
            isPlaying = false;
            isPaused = false;
        }

        public void Pause()
        {
            if (player != null && isPlaying)
            {
                player.Pause();
                isPaused = true;
            }
        }

        public void Resume()
        {
            if (player != null && isPaused)
            {
                player.Resume(CriAtomEx.ResumeMode.AllPlayback);
                isPaused = false;
            }
        }

        public void Seek(float timeMs)
        {
            Debug.LogWarning("[CriAudioManager] Seek not implemented.");
        }

        // Alias methods for unified API
        public void PlaySong(string cueSheet, string cue)
        {
            SetCue(cueSheet, cue);
            Play();
        }

        public void StopSong()
        {
            Stop();
        }

        public float GetCurrentTime()
        {
            return CurrentTimeSec;
        }
    }
}
