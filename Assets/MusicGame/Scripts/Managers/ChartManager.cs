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
            if (string.IsNullOrEmpty(jsonPath))
            {
                Debug.LogWarning("[ChartManager] Chart path is empty.");
                return null;
            }

            TextAsset textAsset = Resources.Load<TextAsset>(jsonPath);
            if (textAsset == null)
            {
                Debug.LogError($"[ChartManager] Chart not found at Resources/{jsonPath}");
                return null;
            }

            try
            {
                ChartData chart = ScriptableObject.CreateInstance<ChartData>();
                JsonUtility.FromJsonOverwrite(textAsset.text, chart);
                if (chart.notes == null || chart.notes.Count == 0)
                {
                    // Try wrapper format
                    string wrappedJson = $"{{\"notes\":{textAsset.text}}}";
                    JsonUtility.FromJsonOverwrite(wrappedJson, chart);
                }
                Debug.Log($"[ChartManager] Loaded chart with {chart.notes.Count} notes.");
                return chart;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChartManager] Failed to parse chart JSON: {e.Message}");
                return null;
            }
        }
    }
}
