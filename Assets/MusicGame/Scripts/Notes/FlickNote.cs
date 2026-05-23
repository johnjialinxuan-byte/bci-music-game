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

        public override void Initialize(NoteData data)
        {
            base.Initialize(data);

            if (arrowTransform != null)
            {
                float angle = GetDirectionAngle(data.flickDirection);
                arrowTransform.rotation = Quaternion.Euler(0, 0, angle + arrowRotationOffset);
            }
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

            FlickDirection detectedDir = InputManager.Instance.DetectFlickDirection();
            if (detectedDir != Data.flickDirection)
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
    }
}
