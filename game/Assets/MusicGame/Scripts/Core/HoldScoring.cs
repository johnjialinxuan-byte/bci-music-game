using System.Collections.Generic;
using UnityEngine;

namespace MusicGame.Core
{
    /// <summary>
    /// Shared math for a Hold's scoreable elements. Both HoldNote (visuals and
    /// runtime judgment) and ScoreManager (total-weight precomputation) use this,
    /// so the number of scored rounds always equals the round dots on screen.
    /// </summary>
    public static class HoldScoring
    {
        public const float RoundInterval = 0.025f;
        public const int MaxRounds = 192;

        /// <summary>Intermediate click checkpoints (excluding the head).</summary>
        public static int CountCheckpoints(NoteData note)
        {
            if (note.noteType != NoteType.Hold || note.attentionPoints == null)
                return 0;
            return note.attentionPoints.Count;
        }

        /// <summary>Click times: head + checkpoints, ascending.</summary>
        public static List<float> GetClickTimes(NoteData note)
        {
            var times = new List<float> { note.time };
            if (note.attentionPoints != null)
            {
                foreach (NotePathPoint point in note.attentionPoints)
                    times.Add(Mathf.Clamp(note.time + point.timeOffset, note.time, note.EndTime));
            }

            times.Sort();
            return times;
        }

        /// <summary>Path node times: click times + the hold end, ascending.</summary>
        public static List<float> GetNodeTimes(NoteData note)
        {
            List<float> times = GetClickTimes(note);
            times.Add(note.EndTime);
            times.Sort();
            return times;
        }

        /// <summary>
        /// Number of round filler dots a Hold generates. Mirrors
        /// HoldNote.BuildVisualFillPieces exactly.
        /// </summary>
        public static int CountRounds(NoteData note)
        {
            if (note.noteType != NoteType.Hold)
                return 0;

            List<float> nodes = GetNodeTimes(note);
            int total = 0;
            for (int segment = 0; segment < nodes.Count - 1 && total < MaxRounds; segment++)
            {
                float duration = Mathf.Max(0f, nodes[segment + 1] - nodes[segment]);
                int count = Mathf.Max(0, Mathf.CeilToInt(duration / RoundInterval) - 1);
                total = Mathf.Min(MaxRounds, total + count);
            }

            return total;
        }
    }
}
