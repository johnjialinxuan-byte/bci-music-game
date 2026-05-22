using UnityEngine;

namespace MusicGame.Core
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int PerfectCount { get; private set; }
        public int GoodCount { get; private set; }
        public int MissCount { get; private set; }
        public float Accuracy { get; private set; }

        public int TotalNotes { get; private set; }
        public int JudgedNotes { get; private set; }

        private const int PerfectScore = 300;
        private const int GoodScore = 150;

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

        public void Initialize(int totalNotes)
        {
            Score = 0;
            Combo = 0;
            MaxCombo = 0;
            PerfectCount = 0;
            GoodCount = 0;
            MissCount = 0;
            Accuracy = 100f;
            TotalNotes = totalNotes;
            JudgedNotes = 0;
        }

        public void RegisterJudgment(JudgmentType judgment)
        {
            JudgedNotes++;
            int points = 0;
            switch (judgment)
            {
                case JudgmentType.Perfect:
                    Combo++;
                    PerfectCount++;
                    points = PerfectScore;
                    break;
                case JudgmentType.Good:
                    Combo++;
                    GoodCount++;
                    points = GoodScore;
                    break;
                case JudgmentType.Miss:
                    Combo = 0;
                    MissCount++;
                    points = 0;
                    break;
            }

            if (Combo > MaxCombo)
                MaxCombo = Combo;

            Score += points + (int)(points * (Combo / 100f));
            UpdateAccuracy();
        }

        private void UpdateAccuracy()
        {
            if (TotalNotes <= 0) return;
            float weightedSum = PerfectCount * 1f + GoodCount * 0.5f;
            Accuracy = (weightedSum / TotalNotes) * 100f;
        }
    }
}
