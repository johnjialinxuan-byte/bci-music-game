using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MusicGame.Core;

namespace MusicGame.Managers
{
    public class ChartManager : MonoBehaviour
    {
        // Player-made charts dropped under Assets/<DiyFolder> override built-in
        // Resources charts. Files are the scorewriter's native save format and
        // are matched to a song by the songId stored inside the JSON.
        private const string DiyFolder = "MusicGame/diy";

        public static ChartManager Instance { get; private set; }

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

        /// <summary>
        /// Loads the chart for a song: a matching DIY chart (scorewriter file in
        /// Assets/MusicGame/diy) wins over the built-in Resources chart.
        /// </summary>
        public ChartData LoadChart(string jsonPath, string songId, Difficulty difficulty)
        {
            ChartData diyChart = TryLoadDiyChart(songId, difficulty);
            if (diyChart != null)
                return diyChart;

            return LoadChart(jsonPath);
        }

        public ChartData LoadChart(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                Debug.LogError("[ChartManager] Chart path is empty.");
                return null;
            }

            TextAsset textAsset = Resources.Load<TextAsset>(jsonPath);
            if (textAsset == null)
            {
                Debug.LogError($"[ChartManager] Chart not found at Resources/{jsonPath}.json");
                return null;
            }

            return LoadChartFromJson(textAsset.text, jsonPath);
        }

        public ChartData LoadChart(TextAsset textAsset)
        {
            if (textAsset == null)
            {
                Debug.LogError("[ChartManager] Cannot load a null chart TextAsset.");
                return null;
            }

            return LoadChartFromJson(textAsset.text, textAsset.name);
        }

        public ChartData LoadChartFromJson(string json, string sourceName = "runtime")
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError($"[ChartManager] Chart JSON is empty: {sourceName}");
                return null;
            }

            try
            {
                ChartData chart = ScriptableObject.CreateInstance<ChartData>();
                JsonUtility.FromJsonOverwrite(json, chart);

                if ((chart.notes == null || chart.notes.Count == 0) && json.TrimStart().StartsWith("["))
                {
                    JsonUtility.FromJsonOverwrite($"{{\"notes\":{json}}}", chart);
                }

                if (!ValidateChart(chart, sourceName))
                {
                    Destroy(chart);
                    return null;
                }

                ApplyGameplaySettings(chart);
                chart.notes.Sort((left, right) => left.time.CompareTo(right.time));
                Debug.Log($"[ChartManager] Loaded JSON chart '{sourceName}' with {chart.notes.Count} notes.");
                return chart;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ChartManager] Failed to parse chart JSON '{sourceName}': {exception.Message}");
                return null;
            }
        }

        private static void ApplyGameplaySettings(ChartData chart)
        {
            if (chart == null || chart.notes == null) return;

            float delaySeconds = GameplaySettings.ChartDelayMs / 1000f;
            float speedScale = 2f / Mathf.Max(0.1f, GameplaySettings.ChartSpeed);
            foreach (NoteData note in chart.notes)
            {
                if (note == null) continue;

                note.time = Mathf.Max(0f, note.time + delaySeconds);
                note.approachTime = Mathf.Max(0.05f, note.approachTime * speedScale);
            }
        }

        private static bool ValidateChart(ChartData chart, string sourceName)
        {
            if (chart.notes == null || chart.notes.Count == 0)
            {
                Debug.LogError($"[ChartManager] Chart contains no notes: {sourceName}");
                return false;
            }

            for (int i = 0; i < chart.notes.Count; i++)
            {
                NoteData note = chart.notes[i];
                if (note == null)
                {
                    Debug.LogError($"[ChartManager] Null note at index {i}: {sourceName}");
                    return false;
                }

                if (note.time < 0f || note.duration < 0f)
                {
                    Debug.LogError($"[ChartManager] Note {i} has a negative time or duration: {sourceName}");
                    return false;
                }

                if (note.noteType == NoteType.Hold && note.duration <= 0f)
                {
                    Debug.LogError($"[ChartManager] Hold note {i} requires a positive duration: {sourceName}");
                    return false;
                }

                if (note.approachTime <= 0f)
                {
                    note.approachTime = 2f;
                }
            }

            return true;
        }

        // ---------- DIY (scorewriter native format) support ----------

        private ChartData TryLoadDiyChart(string songId, Difficulty difficulty)
        {
            if (string.IsNullOrWhiteSpace(songId))
                return null;

            string folder = Path.Combine(Application.dataPath, DiyFolder);
            if (!Directory.Exists(folder))
                return null;

            List<string> matches = new List<string>();
            foreach (string file in Directory.GetFiles(folder, "*.json"))
            {
                string json;
                try { json = File.ReadAllText(file); }
                catch (Exception) { continue; }

                if (!LooksLikeScorewriterChart(json))
                    continue;

                ScorewriterChartDto dto = ParseScorewriterChart(json, file);
                if (dto == null || !string.Equals(dto.songId, songId, StringComparison.OrdinalIgnoreCase))
                    continue;

                matches.Add(file);
            }

            if (matches.Count == 0)
                return null;

            // DIY charts are matched to a difficulty by a difficulty word in the
            // file name (the difficulty field inside scorewriter saves is unreliable
            // — the editor always writes 0).
            string difficultyName = difficulty.ToString().ToLowerInvariant();
            string chosen = matches.Find(f => Path.GetFileNameWithoutExtension(f).ToLowerInvariant().Contains(difficultyName));
            if (chosen == null)
            {
                // If this song has any difficulty-tagged DIY chart but none for the
                // requested difficulty, fall back to the built-in chart rather than
                // serving the wrong difficulty (e.g. Easy must not load the Hard DIY).
                if (matches.Exists(HasDifficultyTag))
                    return null;
                // Legacy: a single untagged DIY file still serves every difficulty.
                chosen = matches[0];
            }

            ScorewriterChartDto chartDto = ParseScorewriterChart(File.ReadAllText(chosen), chosen);
            if (chartDto == null)
                return null;

            ChartData chart = ConvertScorewriterChart(chartDto, Path.GetFileName(chosen), difficulty);
            if (chart != null)
                Debug.Log($"[ChartManager] Using DIY chart '{Path.GetFileName(chosen)}' for song '{songId}' ({chart.notes.Count} notes).");
            return chart;
        }

        private static bool HasDifficultyTag(string filePath)
        {
            string name = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
            return name.Contains("easy") || name.Contains("normal") || name.Contains("hard");
        }

        private static bool LooksLikeScorewriterChart(string json)
        {
            return !string.IsNullOrEmpty(json)
                && json.Contains("\"startLane\"")
                && json.Contains("\"kind\"");
        }

        private static ScorewriterChartDto ParseScorewriterChart(string json, string sourceFile)
        {
            try
            {
                return JsonUtility.FromJson<ScorewriterChartDto>(json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ChartManager] Failed to parse DIY chart '{sourceFile}': {exception.Message}");
                return null;
            }
        }

        private ChartData ConvertScorewriterChart(ScorewriterChartDto dto, string sourceName, Difficulty difficulty)
        {
            if (dto.notes == null || dto.notes.Count == 0)
            {
                Debug.LogError($"[ChartManager] DIY chart contains no notes: {sourceName}");
                return null;
            }

            ChartData chart = ScriptableObject.CreateInstance<ChartData>();
            chart.difficulty = difficulty;
            chart.level = dto.level;

            float offsetSeconds = dto.timingOffsetMs / 1000f;
            float approachTime = DiyApproachTime(difficulty);
            foreach (ScorewriterNoteDto note in dto.notes)
            {
                if (note == null) continue;
                chart.notes.Add(ConvertScorewriterNote(note, offsetSeconds, approachTime));
            }

            if (!ValidateChart(chart, sourceName))
            {
                Destroy(chart);
                return null;
            }

            ApplyGameplaySettings(chart);
            chart.notes.Sort((left, right) => left.time.CompareTo(right.time));
            return chart;
        }

        // DIY charts approach faster than the built-in 2s default and scale with
        // the selected difficulty; lower = faster.
        private static float DiyApproachTime(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Easy => 1.4f,
                Difficulty.Hard => 0.9f,
                _ => 1.2f
            };
        }

        // kind Hold(0)/Round(2) → Hold, Slide(1) → Flick; lanes → world positions.
        // Flick direction (and thus the in-game color) comes straight from the
        // author's noteColor choice — the enums align 1:1: White(0)=Left,
        // Miku(1)=Right, Red(2)=Up, Blue(3)=Down. Deriving it from lane deltas
        // (like the scorewriter's exporter does) guesses wrong whenever a note
        // starts and ends on the same lane.
        private static NoteData ConvertScorewriterNote(ScorewriterNoteDto note, float offsetSeconds, float approachTime)
        {
            Vector3 start = LaneToWorldPosition(note.startLane);
            Vector3 end = LaneToWorldPosition(note.endLane);
            bool isSlide = note.kind == 1;
            bool isRound = note.kind == 2;
            bool isHold = note.kind == 0;

            float duration = isHold
                ? Mathf.Max(0.1f, note.duration)
                : isRound ? 0.1f : 0f;

            return new NoteData
            {
                time = Mathf.Max(0f, note.time - offsetSeconds),
                x = start.x,
                y = start.y,
                z = start.z,
                noteType = isSlide ? NoteType.Flick : NoteType.Hold,
                isRoundNote = isRound,
                duration = duration,
                threshold = Mathf.Max(0, note.threshold),
                hasTailFlick = isHold && note.hasTailSlide,
                flickDirection = (FlickDirection)Mathf.Clamp(note.noteColor, 0, 3),
                approachTime = approachTime,
                useCustomEndPoint = isHold && note.endLane != note.startLane,
                endX = end.x,
                endY = end.y,
                endZ = end.z
            };
        }

        // Lane layout copied from ScorewriterLaneUtility:
        // 0 TopLeft, 1 TopRight, 2 BottomLeft, 3 BottomRight, 4 Center.
        private static Vector3 LaneToWorldPosition(int lane)
        {
            const float x = 2.6f;
            const float y = 1.8f;
            const float z = 10f;
            return lane switch
            {
                0 => new Vector3(-x, y, z),
                1 => new Vector3(x, y, z),
                2 => new Vector3(-x, -y, z),
                3 => new Vector3(x, -y, z),
                _ => new Vector3(0f, 0f, z)
            };
        }

        [Serializable]
        private class ScorewriterChartDto
        {
            public string songId;
            public float timingOffsetMs;
            public int difficulty;
            public int level = 1;
            public List<ScorewriterNoteDto> notes = new List<ScorewriterNoteDto>();
        }

        [Serializable]
        private class ScorewriterNoteDto
        {
            public float time;
            public int kind;
            public int startLane;
            public int endLane;
            public int noteColor;
            public float duration;
            public int threshold = 10;
            public bool hasTailSlide;
        }
    }
}
