using UnityEngine;

namespace MusicGame.Core
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        // Fixed-total scoring: an all-Perfect play of ANY chart scores exactly
        // MaxScore. Each judgment contributes its category weight, normalized by
        // the chart's total weight. Weights: click > flick >> round.
        private const int MaxScore = 1000000;
        private const float ClickWeight = 10f;
        private const float FlickWeight = 8f;
        private const float RoundWeight = 1f;
        private const float GoodFactor = 0.6f;

        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int PerfectCount { get; private set; }
        public int GoodCount { get; private set; }
        public int MissCount { get; private set; }
        public float Accuracy { get; private set; }

        public int TotalNotes { get; private set; }
        public int JudgedNotes { get; private set; }

        private float totalWeight;
        private float earnedWeight;

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

        public void Initialize(ChartData chart)
        {
            Score = 0;
            Combo = 0;
            MaxCombo = 0;
            PerfectCount = 0;
            GoodCount = 0;
            MissCount = 0;
            Accuracy = 100f;
            JudgedNotes = 0;
            earnedWeight = 0f;

            totalWeight = 0f;
            TotalNotes = 0;
            if (chart == null || chart.notes == null)
                return;

            foreach (NoteData note in chart.notes)
            {
                if (note == null) continue;

                if (note.noteType == NoteType.Flick)
                {
                    totalWeight += FlickWeight;
                    TotalNotes++;
                    continue;
                }

                // Hold: head click + checkpoint clicks + filler rounds (+ tail flick).
                totalWeight += note.isRoundNote ? RoundWeight : ClickWeight;
                TotalNotes++;

                int checkpoints = HoldScoring.CountCheckpoints(note);
                totalWeight += checkpoints * ClickWeight;
                TotalNotes += checkpoints;

                int rounds = HoldScoring.CountRounds(note);
                totalWeight += rounds * RoundWeight;
                TotalNotes += rounds;

                if (note.HasTailFlick)
                {
                    totalWeight += FlickWeight;
                    TotalNotes++;
                }
            }
        }

        public void RegisterJudgment(NoteCategory category, JudgmentType judgment)
        {
            JudgedNotes++;
            float weight = CategoryWeight(category);
            switch (judgment)
            {
                case JudgmentType.Perfect:
                    Combo++;
                    PerfectCount++;
                    earnedWeight += weight;
                    break;
                case JudgmentType.Good:
                    Combo++;
                    GoodCount++;
                    earnedWeight += weight * GoodFactor;
                    break;
                case JudgmentType.Miss:
                    Combo = 0;
                    MissCount++;
                    break;
            }

            if (Combo > MaxCombo)
                MaxCombo = Combo;

            Score = totalWeight > 0f
                ? Mathf.RoundToInt(earnedWeight / totalWeight * MaxScore)
                : 0;
            UpdateAccuracy();
        }

        private static float CategoryWeight(NoteCategory category)
        {
            return category switch
            {
                NoteCategory.Click => ClickWeight,
                NoteCategory.Flick => FlickWeight,
                _ => RoundWeight
            };
        }

        private void UpdateAccuracy()
        {
            if (TotalNotes <= 0) return;
            float weightedSum = PerfectCount * 1f + GoodCount * 0.5f;
            Accuracy = (weightedSum / TotalNotes) * 100f;
        }
    }
}
