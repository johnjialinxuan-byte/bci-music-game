using System;
using System.IO;
using CriWare;
using UnityEngine;

namespace Scorewriter
{
    public sealed class ScorewriterCriAudioPlayer : MonoBehaviour
    {
        private const uint TimeStretchVoicePoolId = 0x53435752;

        private CriAtomExPlayer player;
        private CriAtomExPlayback playback;
        private CriAtomExStandardVoicePool timeStretchVoicePool;
        private string registeredAcfPath;
        private string loadedCueSheetName;
        private float manualTime;
        private float playbackRate = 1f;
        private bool isPlaying;
        private bool isPaused;
        private bool triedCreateTimeStretchVoicePool;

        public bool IsPlaying => isPlaying && !isPaused;
        public bool IsPaused => isPaused;
        public float PlaybackRate => playbackRate;

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

            if (timeStretchVoicePool != null)
            {
                timeStretchVoicePool.Dispose();
                timeStretchVoicePool = null;
            }
        }

        public void SetManualTime(float time)
        {
            manualTime = Mathf.Max(0f, time);
        }

        public void SetPlaybackRate(float rate)
        {
            playbackRate = Mathf.Clamp(rate, 0.25f, 1.5f);
            ApplyPlaybackRate();
        }

        private void ApplyPlaybackRate()
        {
            if (player == null)
                return;

            float pitch = 1200f * Mathf.Log(playbackRate, 2f);
            player.SetPlaybackRatio(playbackRate);
            player.SetPitch(pitch);
            player.SetDspTimeStretchRatio(1f / playbackRate);
            if (isPlaying && playback.id != CriAtomExPlayback.invalidId)
            {
                player.Update(playback);
                player.UpdateAll();
            }
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
            EnsureTimeStretchVoicePool();
            player.Loop(false);
            ApplyPlaybackRate();
            player.SetStartTime((long)(Mathf.Max(0f, startTime) * 1000f));
            if (string.IsNullOrWhiteSpace(song.cueName))
                player.SetCueIndex(acb, 0);
            else
                player.SetCue(acb, song.cueName);
            playback = player.Start();
            manualTime = Mathf.Max(0f, startTime);
            isPlaying = true;
            isPaused = false;
            ApplyPlaybackRate();
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

            EnsureTimeStretchVoicePool();
        }

        public bool TryGetSongLength(ScorewriterSong song, out float lengthSeconds)
        {
            lengthSeconds = 0f;
            if (song == null)
                return false;

            EnsureInitialized(song);
            CriAtomExAcb acb = CriAtom.GetAcb(song.cueSheetName);
            if (acb == null)
                return false;

            CriAtomEx.CueInfo cueInfo;
            bool hasCueInfo = string.IsNullOrWhiteSpace(song.cueName)
                ? acb.GetCueInfoByIndex(0, out cueInfo)
                : acb.GetCueInfo(song.cueName, out cueInfo);
            if (hasCueInfo && cueInfo.length > 0)
            {
                lengthSeconds = cueInfo.length / 1000f;
                if (lengthSeconds > 0f)
                    return true;
            }

            CriAtomEx.WaveformInfo waveformInfo = default;
            bool hasWaveformInfo = !string.IsNullOrWhiteSpace(song.cueName)
                ? acb.GetWaveFormInfo(song.cueName, out waveformInfo)
                : hasCueInfo && acb.GetWaveFormInfo(cueInfo.id, out waveformInfo);
            if (!hasWaveformInfo && hasCueInfo)
                hasWaveformInfo = acb.GetWaveFormInfo(cueInfo.id, out waveformInfo);

            if (!hasWaveformInfo || waveformInfo.samplingRate <= 0 || waveformInfo.numSamples <= 0)
                return false;

            lengthSeconds = waveformInfo.numSamples / (float)waveformInfo.samplingRate;
            return lengthSeconds > 0f;
        }

        private void EnsureTimeStretchVoicePool()
        {
            if (player == null)
                return;

            if (timeStretchVoicePool != null)
            {
                player.SetVoicePoolIdentifier(TimeStretchVoicePoolId);
                return;
            }

            if (triedCreateTimeStretchVoicePool)
                return;

            triedCreateTimeStretchVoicePool = true;
            try
            {
                timeStretchVoicePool = new CriAtomExStandardVoicePool(1, 2, 96000, true, TimeStretchVoicePoolId);
                timeStretchVoicePool.AttachDspTimeStretch();
                player.SetVoicePoolIdentifier(TimeStretchVoicePoolId);
            }
            catch (Exception ex)
            {
                if (timeStretchVoicePool != null)
                {
                    timeStretchVoicePool.Dispose();
                    timeStretchVoicePool = null;
                }

                Debug.LogWarning($"[ScorewriterCriAudioPlayer] Time-stretch voice pool unavailable, falling back to pitch playback rate: {ex.Message}");
            }
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
            {
                Debug.LogError($"[ScorewriterCriAudioPlayer] Missing ACF: {acfPath}");
                return;
            }

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

            if (!string.IsNullOrEmpty(loadedCueSheetName) && loadedCueSheetName != song.cueSheetName)
            {
                CriAtom.RemoveCueSheet(loadedCueSheetName);
                loadedCueSheetName = null;
            }

            if (CriAtom.GetAcb(song.cueSheetName) == null)
            {
                CriAtom.AddCueSheet(song.cueSheetName, acbPath, null);
                Debug.Log($"[ScorewriterCriAudioPlayer] Loaded ACB: {song.cueSheetName} -> {acbPath}");
            }

            loadedCueSheetName = song.cueSheetName;
        }

        private static string ResolveAssetPath(string assetRelativePath)
        {
            if (string.IsNullOrWhiteSpace(assetRelativePath))
                return string.Empty;

            if (Path.IsPathRooted(assetRelativePath))
                return assetRelativePath;

            string streamingPath = Path.Combine(Application.streamingAssetsPath, assetRelativePath);
            if (File.Exists(streamingPath))
                return streamingPath;

            string dataPath = Path.Combine(Application.dataPath, assetRelativePath);
            if (File.Exists(dataPath))
                return dataPath;

            return streamingPath;
        }
    }
}


