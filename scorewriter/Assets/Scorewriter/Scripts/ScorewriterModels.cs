using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scorewriter
{
    public enum ScorewriterNoteKind
    {
        Hold,
        Slide,
        Round
    }

    public enum ScorewriterLane
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center
    }

    public enum ScorewriterNoteColor
    {
        White,
        Miku,
        Red,
        Blue
    }

    [Serializable]
    public sealed class ScorewriterSong
    {
        public string songId;
        public string title;
        public string artist;
        public float bpm = 120f;
        public float songLength = 120f;
        public string cueSheetName;
        public string cueName;
        public string acfAssetPath = "CRI/Public/NewProject.acf";
        public string acbAssetPath;

        public string DisplayName => string.IsNullOrWhiteSpace(title) ? songId : title;
    }

    [Serializable]
    public sealed class ScorewriterNote
    {
        public string id;
        public float time;
        public ScorewriterNoteKind kind;
        public ScorewriterLane startLane;
        public ScorewriterLane endLane;
        public ScorewriterNoteColor noteColor = ScorewriterNoteColor.White;
        public float duration;
        public int threshold = 10;
        public bool hasTailSlide;

        public float EndTime => time + Mathf.Max(0f, duration);
    }

    [Serializable]
    public sealed class ScorewriterChart
    {
        public string songId;
        public string title;
        public float bpm = 120f;
        public float songLength = 120f;
        public float timingOffsetMs;
        public bool showQuarterGrid = true;
        public bool showSixthGrid = true;
        public bool showEighthGrid = true;
        public bool showSixteenthGrid = true;
        public int difficulty;
        public int level = 1;
        public List<ScorewriterNote> notes = new List<ScorewriterNote>();
    }

    [Serializable]
    public sealed class GameChartExport
    {
        public int difficulty;
        public int level;
        public List<GameNoteExport> notes = new List<GameNoteExport>();
    }

    [Serializable]
    public sealed class GamePathPointExport
    {
        public float timeOffset;
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public sealed class GameNoteExport
    {
        public float time;
        public float x;
        public float y;
        public float z;
        public int noteType;
        public float duration;
        public int threshold;
        public bool hasTailFlick;
        public int flickDirection;
        public float approachTime = 2f;
        public bool useCustomEndPoint;
        public float endX;
        public float endY;
        public float endZ;
        public string editorKind;
        public string editorColor;
        public int startLane;
        public int endLane;
        public List<GamePathPointExport> attentionPoints = new List<GamePathPointExport>();
    }

    public static class ScorewriterLaneUtility
    {
        private const float X = 2.6f;
        private const float Y = 1.8f;
        private const float Z = 10f;

        public static Vector3 ToWorldPosition(ScorewriterLane lane)
        {
            switch (lane)
            {
                case ScorewriterLane.TopLeft:
                    return new Vector3(-X, Y, Z);
                case ScorewriterLane.TopRight:
                    return new Vector3(X, Y, Z);
                case ScorewriterLane.BottomLeft:
                    return new Vector3(-X, -Y, Z);
                case ScorewriterLane.BottomRight:
                    return new Vector3(X, -Y, Z);
                case ScorewriterLane.Center:
                    return new Vector3(0f, 0f, Z);
                default:
                    return new Vector3(0f, 0f, Z);
            }
        }

        public static string GetShortName(ScorewriterLane lane)
        {
            switch (lane)
            {
                case ScorewriterLane.TopLeft:
                    return "左上";
                case ScorewriterLane.TopRight:
                    return "右上";
                case ScorewriterLane.BottomLeft:
                    return "左下";
                case ScorewriterLane.BottomRight:
                    return "右下";
                case ScorewriterLane.Center:
                    return "中间";
                default:
                    return "?";
            }
        }

        public static int DirectionFromLanes(ScorewriterLane start, ScorewriterLane end)
        {
            Vector3 delta = ToWorldPosition(end) - ToWorldPosition(start);
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                if (delta.x > 0.01f)
                    return 1; // Right
                if (delta.x < -0.01f)
                    return 0; // Left
            }

            if (delta.y > 0.01f)
                return 2; // Up
            if (delta.y < -0.01f)
                return 3; // Down

            switch (start)
            {
                case ScorewriterLane.TopLeft:
                    return 0;
                case ScorewriterLane.TopRight:
                    return 1;
                case ScorewriterLane.BottomLeft:
                    return 3;
                case ScorewriterLane.BottomRight:
                    return 2;
                default:
                    return 0;
            }
        }
    }
}
