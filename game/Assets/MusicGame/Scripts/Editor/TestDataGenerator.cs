#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using MusicGame.Core;

namespace MusicGame.Editor
{
    public static class TestDataGenerator
    {
        [MenuItem("MusicGame/Generate Test Data")]
        public static void GenerateTestData()
        {
            string songsFolder = "Assets/MusicGame/Resources/Songs";
            string chartsFolder = "Assets/MusicGame/Resources/Charts";
            if (!AssetDatabase.IsValidFolder(songsFolder))
                AssetDatabase.CreateFolder("Assets/MusicGame/Resources", "Songs");
            if (!AssetDatabase.IsValidFolder(chartsFolder))
                AssetDatabase.CreateFolder("Assets/MusicGame/Resources", "Charts");

            SongData song = ScriptableObject.CreateInstance<SongData>();
            song.songId = "test_song_01";
            song.title = "Test Song";
            song.artist = "Test Artist";
            song.bpm = 120f;
            song.previewStartTime = 10f;
            song.cueSheetName = "CueSheet_0";
            song.cueName = "cue_0000";

            song.easyChartPath = "Charts/test_easy";
            song.normalChartPath = "Charts/test_normal";
            song.hardChartPath = "Charts/test_hard";

            WriteChartJson(song.easyChartPath, Difficulty.Easy, 3, 30);
            WriteChartJson(song.normalChartPath, Difficulty.Normal, 5, 60);
            WriteChartJson(song.hardChartPath, Difficulty.Hard, 8, 100);

            AssetDatabase.CreateAsset(song, $"{songsFolder}/TestSong.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TestDataGenerator] Test data generated successfully!");
            Selection.activeObject = song;
        }

        private static void WriteChartJson(string resourcePath, Difficulty difficulty, int level, int noteCount)
        {
            float bpm = 120f;
            float beatInterval = 60f / bpm;
            var notes = new List<NoteData>();

            for (int i = 0; i < noteCount; i++)
            {
                float time = i * beatInterval;
                NoteType type = (i % 3 == 0) ? NoteType.Flick : NoteType.Hold;
                notes.Add(new NoteData
                {
                    time = time,
                    x = Mathf.Sin(i * 0.8f) * 3f,
                    y = Mathf.Cos(i * 0.6f) * 2f,
                    z = 10f,
                    noteType = type,
                    duration = type == NoteType.Hold ? beatInterval * 2 : 0f,
                    flickDirection = type == NoteType.Flick ? RandomFlickDir() : FlickDirection.Left,
                    approachTime = 2f
                });
            }

            ChartData chart = ScriptableObject.CreateInstance<ChartData>();
            chart.difficulty = difficulty;
            chart.level = level;
            chart.notes = notes;

            string json = JsonUtility.ToJson(chart, true);
            string filePath = Path.Combine(Application.dataPath, "MusicGame", "Resources", resourcePath + ".json");
            filePath = Path.GetFullPath(filePath);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, json);
        }

        private static FlickDirection RandomFlickDir()
        {
            System.Array values = System.Enum.GetValues(typeof(FlickDirection));
            return (FlickDirection)values.GetValue(Random.Range(0, values.Length));
        }
    }
}
#endif
