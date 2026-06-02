using UnityEngine;
using MusicGame.Core;

namespace MusicGame.Managers
{
    public class JudgeManager : MonoBehaviour
    {
        public static JudgeManager Instance { get; private set; }

        [Header("Timing Windows (seconds)")]
        [SerializeField] private float perfectWindow = 0.050f;
        [SerializeField] private float goodWindow = 0.120f;
        
        private float flickPerfectWindow;
        private float flickGreatWindow;
[SerializeField] private float missWindow = 0.250f;

        public float PerfectWindow => perfectWindow;
        public float GoodWindow => goodWindow;
        
        public float FlickPerfectWindow => flickPerfectWindow;
        public float FlickGreatWindow => flickGreatWindow;
public float MissWindow => missWindow;

private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            flickPerfectWindow = GameplaySettings.FlickPerfectWindow;
            flickGreatWindow = GameplaySettings.FlickGreatWindow;
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

public JudgmentType JudgeFlick(float timeDifference)
        {
            float absDiff = Mathf.Abs(timeDifference);
            if (absDiff <= flickPerfectWindow)
                return JudgmentType.Perfect;
            if (absDiff <= flickGreatWindow)
                return JudgmentType.Good;
            return JudgmentType.Miss;
        }

        public bool IsInFlickHitWindow(float timeDifference)
        {
            return Mathf.Abs(timeDifference) <= flickGreatWindow;
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
