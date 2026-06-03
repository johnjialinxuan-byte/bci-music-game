using System;
using System.Collections.Generic;
using UnityEngine;

namespace MusicGame.Core
{
    [CreateAssetMenu(fileName = "SongData", menuName = "MusicGame/SongData")]
    public class SongData : ScriptableObject
    {
        public string songId;
        public string title;
        public string artist;
        public float bpm;
        public float previewStartTime;
        public float previewDuration = 30f;
        public Sprite coverImage;
        public string cueSheetName;
        public string cueName;

        [Header("JSON Chart Paths (relative to Resources)")]
        public string easyChartPath;
        public string normalChartPath;
        public string hardChartPath;

        public string GetChartPath(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Easy => easyChartPath,
                Difficulty.Normal => normalChartPath,
                Difficulty.Hard => hardChartPath,
                _ => easyChartPath
            };
        }
    }

    [CreateAssetMenu(fileName = "ChartData", menuName = "MusicGame/ChartData")]
    public class ChartData : ScriptableObject
    {
        public Difficulty difficulty;
        public int level;
        public List<NoteData> notes = new List<NoteData>();
    }
}
