using UnityEngine;
using MusicGame.Core;

namespace MusicGame.Gameplay
{
    public class JudgmentSystem : MonoBehaviour
    {
        public static JudgmentSystem Instance { get; private set; }

        [Header("Timing Windows (seconds)")]
        [SerializeField] private float perfectWindow = 0.050f;
        [SerializeField] private float goodWindow = 0.120f;
        [SerializeField] private float missWindow = 0.250f;

        public float PerfectWindow => perfectWindow;
        public float GoodWindow => goodWindow;
        public float MissWindow => missWindow;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public JudgmentType Judge(float timeDifference)
        {
            float absDiff = Mathf.Abs(timeDifference);
            if (absDiff <= perfectWindow)
                return JudgmentType.Perfect;
            if (absDiff <= goodWindow)
                return JudgmentType.Good;
            if (absDiff <= missWindow)
                return JudgmentType.Miss;
            return JudgmentType.Miss;
        }

        public bool IsInAnyWindow(float timeDifference)
        {
            return Mathf.Abs(timeDifference) <= missWindow;
        }

        public bool IsInHitWindow(float timeDifference)
        {
            return Mathf.Abs(timeDifference) <= goodWindow;
        }
    }
}
