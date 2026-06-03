using System.IO;
using CriWare;
using UnityEngine;

namespace Scorewriter
{
    public sealed class ScorewriterCriAudioPlayer : MonoBehaviour
    {
        private CriAtomExPlayer player;
        private CriAtomExPlayback playback;
        private string registeredAcfPath;
        private string loadedCueSheetName;
        private float manualTime;
        private bool isPlaying;
        private bool isPaused;

        public bool IsPlaying => isPlaying && !isPaused;
        public bool IsPaused => isPaused;

        public float CurrentTime
        {
            get
            {
                if (!isPlaying || playback.id == CriAtomExPlayback.invalidId)
                    return manualTime;

                long syncedTime = playback.GetTimeSyncedWithAudio();
                if (syncedTime < 0)
                    syncedTime = playback.GetTime();

                if (syncedTime < 0)
                    return manualTime;

                manualTime = syncedTime / 1000f;
                return manualTime;
            }
        }

        private void OnDestroy()
        {
            Stop();

            if (player != null)
            {
                player.Dispose();
                player = null;
            }
        }

        public void SetManualTime(float time)
        {
            manualTime = Mathf.Max(0f, time);
        }

        public void Play(ScorewriterSong song, float startTime)
        {
            if (song == null)
                return;

            EnsureInitialized(song);
            CriAtomExAcb acb = CriAtom.GetAcb(song.cueSheetName);
            if (acb == null)
            {
                Debug.LogError($"[ScorewriterCriAudioPlayer] ACB not loaded: {song.cueSheetName}");
                return;
            }

            if (player == null)
                player = new CriAtomExPlayer(true);

            player.Stop();
            player.ResetParameters();
            player.Loop(false);
            player.SetStartTime((long)(Mathf.Max(0f, startTime) * 1000f));
            if (string.IsNullOrWhiteSpace(song.cueName))
                player.SetCueIndex(acb, 0);
            else
                player.SetCue(acb, song.cueName);
            playback = player.Start();
            manualTime = Mathf.Max(0f, startTime);
            isPlaying = true;
            isPaused = false;
        }

        public void Pause()
        {
            if (player == null || !isPlaying)
                return;

            manualTime = CurrentTime;
            player.Pause();
            isPaused = true;
        }

        public void Resume()
        {
            if (player == null || !isPlaying || !isPaused)
                return;

            player.Resume(CriAtomEx.ResumeMode.AllPlayback);
            isPaused = false;
        }

        public void Stop()
        {
            if (player != null)
                player.Stop();

            isPlaying = false;
            isPaused = false;
            manualTime = 0f;
        }

        public void Seek(ScorewriterSong song, float time)
        {
            time = Mathf.Max(0f, time);
            if (isPlaying)
                Play(song, time);
            else
                manualTime = time;
        }

        public void EnsureInitialized(ScorewriterSong song)
        {
            EnsureAtomComponent();
            RegisterAcf(song);
            LoadCueSheet(song);

            if (player == null)
                player = new CriAtomExPlayer(true);
        }

        private static void EnsureAtomComponent()
        {
            if (FindAnyObjectByType<CriAtom>() != null)
                return;

            GameObject atomObject = new GameObject("CRI Atom");
            atomObject.AddComponent<CriAtom>();
            DontDestroyOnLoad(atomObject);
        }

        private void RegisterAcf(ScorewriterSong song)
        {
            string acfPath = ResolveAssetPath(song.acfAssetPath);
            if (string.IsNullOrEmpty(acfPath) || !File.Exists(acfPath))
                return;

            if (registeredAcfPath == acfPath)
                return;

            if (!string.IsNullOrEmpty(registeredAcfPath))
                CriAtomEx.UnregisterAcf();

            CriAtomEx.RegisterAcf(null, acfPath);
            registeredAcfPath = acfPath;
        }

        private void LoadCueSheet(ScorewriterSong song)
        {
            if (string.IsNullOrWhiteSpace(song.cueSheetName))
                return;

            if (loadedCueSheetName == song.cueSheetName && CriAtom.GetAcb(song.cueSheetName) != null)
                return;

            string acbPath = ResolveAssetPath(song.acbAssetPath);
            if (string.IsNullOrEmpty(acbPath) || !File.Exists(acbPath))
            {
                Debug.LogError($"[ScorewriterCriAudioPlayer] Missing ACB: {acbPath}");
                return;
            }

            if (CriAtom.GetAcb(song.cueSheetName) == null)
                CriAtom.AddCueSheet(song.cueSheetName, acbPath, null);

            loadedCueSheetName = song.cueSheetName;
        }

        private static string ResolveAssetPath(string assetRelativePath)
        {
            if (string.IsNullOrWhiteSpace(assetRelativePath))
                return string.Empty;

            if (Path.IsPathRooted(assetRelativePath))
                return assetRelativePath;

            return Path.Combine(Application.dataPath, assetRelativePath);
        }
    }
}
