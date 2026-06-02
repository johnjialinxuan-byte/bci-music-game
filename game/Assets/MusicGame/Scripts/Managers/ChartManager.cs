using System;
using UnityEngine;
using MusicGame.Core;

namespace MusicGame.Managers
{
    public class ChartManager : MonoBehaviour
    {
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
    }
}
