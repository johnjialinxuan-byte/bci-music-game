using UnityEngine;
using MusicGame.Core;
using MusicGame.Gameplay;
using MusicGame.Managers;
using MusicGame.Audio;

namespace MusicGame.Notes
{
    public class FlickNote : NoteBase
    {
        [Header("Flick Visuals")]
        [SerializeField] private Transform arrowTransform;
        [SerializeField] private float arrowRotationOffset = 0f;
        [SerializeField, Range(0.1f, 1f)] private float visualScaleMultiplier = 0.7f;
        public override void Initialize(NoteData data)
        {
            base.Initialize(data);

            if (arrowTransform != null)
            {
                FlickDirection effectiveDirection = GetEffectiveDirection(data.flickDirection);
                float angle = GetDirectionAngle(effectiveDirection);
                arrowTransform.rotation = Quaternion.Euler(0, 0, angle + arrowRotationOffset);
            }
        }

        protected override void UpdatePosition()
        {
            base.UpdatePosition();
            if (visualTransform != null)
                visualTransform.localScale *= visualScaleMultiplier;
        }


protected override void Update()
        {
            base.Update();
            TryHit();
        }


        public void TryHit()
        {
            if (IsJudged || IsMissed) return;

            float timeDiff = SongTime - Data.time;
            if (!JudgeManager.Instance.IsInHitWindow(timeDiff)) return;

            FlickDirection expectedDirection = GetEffectiveDirection(Data.flickDirection);
            FlickDirection detectedDir = InputManager.Instance.DetectFlickDirection();
            if (detectedDir != expectedDirection)
                return;

            JudgmentType judgment = JudgeManager.Instance.Judge(timeDiff);
            OnHit(judgment);
        }

        private float GetDirectionAngle(FlickDirection dir)
        {
            return dir switch
            {
                FlickDirection.Left => 180f,
                FlickDirection.Right => 0f,
                FlickDirection.Up => 90f,
                FlickDirection.Down => -90f,
                _ => 0f
            };
        }
    

        protected override void ShowJudgmentEffect(JudgmentType judgment)
        {
            if (judgment == JudgmentType.Miss) return;
            AudioManager.Instance?.PlaySFX("cuesheet0", "");
        }


        private static FlickDirection GetEffectiveDirection(FlickDirection originalDirection)
        {
            return originalDirection switch
            {
                FlickDirection.Up => FlickDirection.Right,   // red: clockwise by 90 degrees
                FlickDirection.Down => FlickDirection.Right, // blue: counterclockwise by 90 degrees
                _ => originalDirection
            };
        }
}
}
