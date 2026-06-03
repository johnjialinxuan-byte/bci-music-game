using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CriWare;
using MusicGame.Core;

namespace MusicGame.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CriWareInitializer))]
    public sealed class AudioManager : MonoBehaviour
    {
        private const string DefaultAcfFile = "CRI/NewProject1.acf";
        private const string DefaultCueSheetFolder = "CRI/WorkUnit_0";

        public static AudioManager Instance { get; private set; }

        [Header("CRIWARE Files (relative to StreamingAssets)")]
        [SerializeField] private string acfFile = DefaultAcfFile;
        [SerializeField] private string cueSheetFolder = DefaultCueSheetFolder;

        [Header("Default Song")]
        [SerializeField] private string cueSheetName = "";
        [SerializeField] private string cueName = "";
        private readonly HashSet<string> loadedCueSheets = new HashSet<string>();
        private CriAtomExPlayer player;
        private CriAtomExPlayback playback;
        private bool hasPlayback;
        private bool criAtomReady;
        private long currentCueLengthMs;
        private float currentCueSinglePlaybackDurationSec;

        public bool IsCurrentCueIndefinite => currentCueLengthMs < 0;
        public bool HasReachedCurrentCueSinglePlaybackEnd => currentCueSinglePlaybackDurationSec <= 0f || GetCurrentTime() >= currentCueSinglePlaybackDurationSec;
        private bool ownsAcfRegistration;

        private CriAtomExPlayer sfxPlayer;

        private CriAtomExPlayer previewPlayer;
        private CriAtomExPlayback previewPlayback;
        private bool hasPreviewPlayback;
        private Coroutine previewRoutine;
        private SongData currentPreviewSong;
        private float previewVolume;
        private int previewRequestId;

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
            if (Instance != this) return;

            StopSong();
            StopPreviewImmediate();
            foreach (string loadedCueSheet in loadedCueSheets)
            {
                CriAtom.RemoveCueSheet(loadedCueSheet);
            }
            loadedCueSheets.Clear();

            player?.Dispose();
            player = null;

            previewPlayer?.Dispose();
            previewPlayer = null;

            if (ownsAcfRegistration)
            {
                CriAtomEx.UnregisterAcf();
                ownsAcfRegistration = false;
            }

            Instance = null;
        }

        public void PlaySong()
        {
            PlaySong(cueSheetName, cueName);
        }

        public void PlaySong(SongData song)
        {
            if (song == null)
            {
                Debug.LogError("[AudioManager] Cannot play a null song.");
                return;
            }

            PlaySong(song.cueSheetName, song.cueName);
        }

        public void PlaySong(string cueSheet, string cue)
        {
            if (string.IsNullOrWhiteSpace(cueSheet))
            {
                Debug.LogError("[AudioManager] A CRIWARE cue sheet name is required.");
                return;
            }

            StopSong();
            currentCueLengthMs = 0;
            currentCueSinglePlaybackDurationSec = 0f;
            cueSheetName = cueSheet;
            cueName = cue;

            if (!EnsureCueSheetLoaded(cueSheetName)) return;

            CriAtomExAcb acb = CriAtom.GetAcb(cueSheetName);
            if (acb == null)
            {
                Debug.LogError($"[AudioManager] Failed to obtain CRIWARE ACB: {cueSheetName}");
                return;
            }

            bool playFirstCueByIndex = false;
            CriAtomEx.CueInfo cueInfo;
            bool hasCueInfo;
            if (string.IsNullOrWhiteSpace(cueName))
            {
                CriAtomEx.CueInfo[] cueInfos = acb.GetCueInfoList();
                if (cueInfos == null || cueInfos.Length == 0)
                {
                    Debug.LogError($"[AudioManager] Cue sheet '{cueSheetName}' contains no cues.");
                    return;
                }

                cueInfo = cueInfos[0];
                hasCueInfo = true;
                cueName = cueInfo.name;
                playFirstCueByIndex = string.IsNullOrWhiteSpace(cueName);
            }
            else
            {
                hasCueInfo = acb.GetCueInfo(cueName, out cueInfo);
            }

            if (hasCueInfo)
            {
                currentCueLengthMs = cueInfo.length;
                if (currentCueLengthMs > 0)
                {
                    currentCueSinglePlaybackDurationSec = currentCueLengthMs / 1000f;
                }
                else
                {
                    CriAtomEx.WaveformInfo waveformInfo;
                    bool hasWaveformInfo = playFirstCueByIndex
                        ? acb.GetWaveFormInfo(cueInfo.id, out waveformInfo)
                        : acb.GetWaveFormInfo(cueName, out waveformInfo);
                    if (hasWaveformInfo && waveformInfo.samplingRate > 0 && waveformInfo.numSamples > 0)
                        currentCueSinglePlaybackDurationSec = (float)waveformInfo.numSamples / waveformInfo.samplingRate;

                    Debug.Log($"[AudioManager] Cue '{cueSheetName}/{cueName}' is indefinite; gameplay will end at its single-playback duration ({currentCueSinglePlaybackDurationSec:F3}s) after its chart completes.");
                }
            }

            player ??= new CriAtomExPlayer(true);
            player.Loop(false);
            if (playFirstCueByIndex)
            {
                player.SetCueIndex(acb, 0);
            }
            else
            {
                player.SetCue(acb, cueName);
            }
            playback = player.Start();
            hasPlayback = playback.id != CriAtomExPlayback.invalidId;

            if (!hasPlayback)
            {
                string playableCue = playFirstCueByIndex ? "#0" : cueName;
                Debug.LogError($"[AudioManager] CRIWARE failed to start '{cueSheetName}/{playableCue}'.");
            }
        }

        public void PlaySongPreview(SongData song, float fadeDuration = 0.45f)
        {
            previewRequestId++;
            if (previewRoutine != null)
                StopCoroutine(previewRoutine);
            previewRoutine = StartCoroutine(SwitchPreviewRoutine(song, Mathf.Max(0f, fadeDuration), previewRequestId));
        }

        public void StopSongPreview(float fadeDuration = 0.35f)
        {
            previewRequestId++;
            if (previewRoutine != null)
                StopCoroutine(previewRoutine);
            previewRoutine = StartCoroutine(StopPreviewRoutine(Mathf.Max(0f, fadeDuration), previewRequestId));
        }

        public void StopPreviewImmediate()
        {
            previewRequestId++;
            if (previewRoutine != null)
            {
                StopCoroutine(previewRoutine);
                previewRoutine = null;
            }
            currentPreviewSong = null;
            StopPreviewPlayback();
        }

        private IEnumerator StopPreviewRoutine(float fadeDuration, int requestId)
        {
            currentPreviewSong = null;
            if (hasPreviewPlayback)
                yield return FadePreviewTo(0f, fadeDuration, requestId);
            if (requestId == previewRequestId)
                StopPreviewPlayback();
        }

        private IEnumerator SwitchPreviewRoutine(SongData song, float fadeDuration, int requestId)
        {
            currentPreviewSong = song;

            if (hasPreviewPlayback)
            {
                yield return FadePreviewTo(0f, fadeDuration, requestId);
                if (requestId != previewRequestId) yield break;
                StopPreviewPlayback();
            }

            if (song == null || string.IsNullOrWhiteSpace(song.cueSheetName))
                yield break;

            while (requestId == previewRequestId && currentPreviewSong == song)
            {
                float segmentDuration = StartPreviewSegment(song);
                if (segmentDuration <= 0f)
                    yield break;

                yield return FadePreviewTo(1f, fadeDuration, requestId);
                if (requestId != previewRequestId) yield break;

                float holdDuration = Mathf.Max(0.1f, segmentDuration - fadeDuration * 2f);
                float elapsed = 0f;
                while (elapsed < holdDuration && requestId == previewRequestId && currentPreviewSong == song)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (requestId != previewRequestId || currentPreviewSong != song)
                    yield break;

                yield return FadePreviewTo(0f, fadeDuration, requestId);
                if (requestId != previewRequestId) yield break;
                StopPreviewPlayback();
            }
        }

        private float StartPreviewSegment(SongData song)
        {
            if (song == null || string.IsNullOrWhiteSpace(song.cueSheetName))
                return 0f;
            if (!EnsureCueSheetLoaded(song.cueSheetName))
                return 0f;

            CriAtomExAcb acb = CriAtom.GetAcb(song.cueSheetName);
            if (acb == null)
            {
                Debug.LogError($"[AudioManager] Preview ACB not found: {song.cueSheetName}");
                return 0f;
            }

            bool playFirstCueByIndex = string.IsNullOrWhiteSpace(song.cueName);
            CriAtomEx.CueInfo cueInfo;
            bool hasCueInfo;
            string playableCue = song.cueName;
            if (playFirstCueByIndex)
            {
                CriAtomEx.CueInfo[] cueInfos = acb.GetCueInfoList();
                if (cueInfos == null || cueInfos.Length == 0)
                    return 0f;
                cueInfo = cueInfos[0];
                playableCue = cueInfo.name;
                hasCueInfo = true;
                playFirstCueByIndex = string.IsNullOrWhiteSpace(playableCue);
            }
            else
            {
                hasCueInfo = acb.GetCueInfo(song.cueName, out cueInfo);
            }

            float cueDuration = 0f;
            if (hasCueInfo && cueInfo.length > 0)
            {
                cueDuration = cueInfo.length / 1000f;
            }
            else if (hasCueInfo)
            {
                CriAtomEx.WaveformInfo waveformInfo;
                bool hasWaveformInfo = playFirstCueByIndex
                    ? acb.GetWaveFormInfo(cueInfo.id, out waveformInfo)
                    : acb.GetWaveFormInfo(playableCue, out waveformInfo);
                if (hasWaveformInfo && waveformInfo.samplingRate > 0 && waveformInfo.numSamples > 0)
                    cueDuration = (float)waveformInfo.numSamples / waveformInfo.samplingRate;
            }

            float previewStart = Mathf.Max(0f, song.previewStartTime);
            float requestedDuration = song.previewDuration > 0f ? song.previewDuration : 30f;
            float segmentDuration = requestedDuration;
            if (cueDuration > 0f)
            {
                if (previewStart >= cueDuration)
                    previewStart = 0f;
                segmentDuration = Mathf.Max(1f, Mathf.Min(requestedDuration, cueDuration - previewStart));
            }

            previewPlayer ??= new CriAtomExPlayer(true);
            previewPlayer.Stop();
            previewPlayer.Loop(false);
            previewPlayer.SetVolume(0f);
            previewPlayer.SetStartTime((long)(previewStart * 1000f));
            if (playFirstCueByIndex)
                previewPlayer.SetCueIndex(acb, 0);
            else
                previewPlayer.SetCue(acb, playableCue);

            previewPlayback = previewPlayer.Start();
            hasPreviewPlayback = previewPlayback.id != CriAtomExPlayback.invalidId;
            previewVolume = 0f;
            return hasPreviewPlayback ? segmentDuration : 0f;
        }

private IEnumerator FadePreviewTo(float targetVolume, float duration, int requestId)
        {
            if (!hasPreviewPlayback || previewPlayer == null)
                yield break;

            float startVolume = previewVolume;
            if (duration <= 0f)
            {
                ApplyPreviewVolume(targetVolume);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration && requestId == previewRequestId)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                ApplyPreviewVolume(Mathf.Lerp(startVolume, targetVolume, t));
                yield return null;
            }

            if (requestId == previewRequestId)
                ApplyPreviewVolume(targetVolume);
        }

        private void StopPreviewPlayback()
        {
            if (hasPreviewPlayback)
            {
                previewPlayback.Stop();
                hasPreviewPlayback = false;
            }

            previewPlayer?.Stop();
            previewVolume = 0f;
        }

        public void StopSong()
        {
            if (hasPlayback)
            {
                playback.Stop();
                hasPlayback = false;
            }

            player?.Stop();
        }

        public float GetCurrentTime()
        {
            if (!IsPlaying()) return 0f;

            long timeMs = playback.GetTimeSyncedWithAudio();
            if (timeMs < 0)
            {
                timeMs = playback.GetTime();
            }

            return timeMs < 0 ? 0f : timeMs / 1000f;
        }

        public bool IsPlaying()
        {
            if (!hasPlayback || playback.id == CriAtomExPlayback.invalidId) return false;

            CriAtomExPlayback.Status status = playback.GetStatus();
            if (status == CriAtomExPlayback.Status.Removed)
            {
                hasPlayback = false;
                return false;
            }

            return status == CriAtomExPlayback.Status.Prep || status == CriAtomExPlayback.Status.Playing;
        }

        public void Pause()
        {
            if (IsPlaying())
            {
                playback.Pause();
            }
        }

        public void Resume()
        {
            if (hasPlayback)
            {
                playback.Resume(CriAtomEx.ResumeMode.AllPlayback);
            }
        }

        public void PlaySFX(string cueSheet, string cue)
        {
            if (string.IsNullOrWhiteSpace(cueSheet))
            {
                Debug.LogWarning("[AudioManager] PlaySFX called with empty cueSheet.");
                return;
            }

            string acbFilePath = Path.Combine(CriWare.Common.streamingAssetsPath, $"{cueSheetFolder}/{cueSheet}.acb");
            if (!File.Exists(acbFilePath))
            {
                Debug.LogWarning($"[AudioManager] PlaySFX: ACB file not found at {acbFilePath}. Skipping.");
                return;
            }

            if (!EnsureCueSheetLoaded(cueSheet)) return;

            CriAtomExAcb acb = CriAtom.GetAcb(cueSheet);
            if (acb == null)
            {
                Debug.LogError($"[AudioManager] PlaySFX: ACB not found for {cueSheet}.");
                return;
            }

            bool playFirstCueByIndex = string.IsNullOrWhiteSpace(cue);
            if (playFirstCueByIndex)
            {
                CriAtomEx.CueInfo[] cueInfos = acb.GetCueInfoList();
                if (cueInfos == null || cueInfos.Length == 0)
                {
                    Debug.LogWarning($"[AudioManager] PlaySFX: Cue sheet '{cueSheet}' contains no cues.");
                    return;
                }
            }

            sfxPlayer ??= new CriAtomExPlayer(true);
            if (playFirstCueByIndex)
                sfxPlayer.SetCueIndex(acb, 0);
            else
                sfxPlayer.SetCue(acb, cue);
            sfxPlayer.Start();
        }

        private bool EnsureCueSheetLoaded(string cueSheet)
        {
            if (!EnsureCriAtomReady()) return false;
            if (loadedCueSheets.Contains(cueSheet)) return true;

            string acbPath = $"{cueSheetFolder}/{cueSheet}.acb";
            CriAtom.AddCueSheet(cueSheet, acbPath, string.Empty);
            if (CriAtom.GetAcb(cueSheet) != null)
            {
                loadedCueSheets.Add(cueSheet);
                return true;
            }

            Debug.LogError($"[AudioManager] Could not load CRIWARE cue sheet at StreamingAssets/{acbPath}.");
            return false;
        }

        private bool EnsureCriAtomReady()
        {
            if (criAtomReady) return true;

            if (!CriAtomPlugin.IsLibraryInitialized())
            {
                CriAtomPlugin.InitializeLibrary();
            }

            if (GetComponent<CriAtom>() == null)
            {
                gameObject.AddComponent<CriAtom>();
            }

            if (!string.IsNullOrWhiteSpace(acfFile))
            {
                string acfPath = Path.Combine(CriWare.Common.streamingAssetsPath, acfFile);
                ownsAcfRegistration = CriAtomEx.RegisterAcf(null, acfPath);
                if (!ownsAcfRegistration)
                {
                    Debug.LogError($"[AudioManager] Failed to register CRIWARE ACF at StreamingAssets/{acfFile}.");
                    return false;
                }
            }

            criAtomReady = true;
            return true;
        }
    

private void ApplyPreviewVolume(float volume)
        {
            previewVolume = volume;
            if (previewPlayer == null) return;

            previewPlayer.SetVolume(previewVolume);
            if (hasPreviewPlayback)
                previewPlayer.Update(previewPlayback);
        }
}
}
