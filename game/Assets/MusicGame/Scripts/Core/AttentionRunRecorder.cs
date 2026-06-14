using System.Collections.Generic;
using UnityEngine;

namespace MusicGame.Core
{
    public readonly struct AttentionSample
    {
        public AttentionSample(float time, int value)
        {
            Time = time;
            Value = Mathf.Clamp(value, 0, 100);
        }

        public float Time { get; }
        public int Value { get; }
    }

    public static class AttentionRunRecorder
    {
        private static readonly List<AttentionSample> samples = new List<AttentionSample>(1024);
        private static float startedAt;
        private static int peak;
        private static float sum;

        public static IReadOnlyList<AttentionSample> Samples => samples;
        public static int Peak => peak;
        public static float Average => samples.Count > 0 ? sum / samples.Count : 0f;
        public static float Duration => samples.Count > 0 ? samples[samples.Count - 1].Time : 0f;
        public static bool HasSamples => samples.Count > 0;

        public static void Begin()
        {
            samples.Clear();
            startedAt = Time.unscaledTime;
            peak = 0;
            sum = 0f;
        }

        public static void Record(int attention)
        {
            int value = Mathf.Clamp(attention, 0, 100);
            float time = Mathf.Max(0f, Time.unscaledTime - startedAt);
            samples.Add(new AttentionSample(time, value));
            peak = Mathf.Max(peak, value);
            sum += value;
        }
    }
}
