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
        private const string DefaultAcfFile = "CRI/NewProject.acf";
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
        private bool ownsAcfRegistration;

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
            foreach (string loadedCueSheet in loadedCueSheets)
            {
                CriAtom.RemoveCueSheet(loadedCueSheet);
            }
            loadedCueSheets.Clear();

            player?.Dispose();
            player = null;

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
            cueSheetName = cueSheet;
            cueName = cue;

            if (!EnsureCueSheetLoaded(cueSheetName)) return;

            CriAtomExAcb acb = CriAtom.GetAcb(cueSheetName);
            if (acb == null)
            {
                Debug.LogError($"[AudioManager] Failed to obtain CRIWARE ACB: {cueSheetName}");
                return;
            }

            if (string.IsNullOrWhiteSpace(cueName))
            {
                CriAtomEx.CueInfo[] cueInfos = acb.GetCueInfoList();
                if (cueInfos == null || cueInfos.Length == 0)
                {
                    Debug.LogError($"[AudioManager] Cue sheet '{cueSheetName}' contains no cues.");
                    return;
                }

                cueName = cueInfos[0].name;
            }

            player ??= new CriAtomExPlayer(true);
            player.SetCue(acb, cueName);
            playback = player.Start();
            hasPlayback = playback.id != CriAtomExPlayback.invalidId;

            if (!hasPlayback)
            {
                Debug.LogError($"[AudioManager] CRIWARE failed to start '{cueSheetName}/{cueName}'.");
            }
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

        private bool EnsureCueSheetLoaded(string cueSheet)
        {
            if (!EnsureCriAtomReady()) return false;
            if (CriAtom.GetAcb(cueSheet) != null) return true;

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
    }
}
