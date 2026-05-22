using UnityEngine;
using MusicGame.Core;
using MusicGame.Gameplay;
using MusicGame.Audio;
using MusicGame.Managers;

namespace MusicGame.Notes
{
    public class HoldNote : NoteBase
    {
        [Header("Hold Visuals")]
        [SerializeField] private SpriteRenderer tailSpriteRenderer;
        [SerializeField] private Transform tailTransform;
        [SerializeField] private LineRenderer connectionLine;
        [SerializeField] private float samplingInterval = 0.1f;

        private bool isHolding;
        private bool headJudged;
        private JudgmentType headJudgment;
        private float holdStartTime;
        private float lastSampleTime;
        private float successProgress;

        public float EndTime => Data.EndTime;
        public bool IsHolding => isHolding;

        public override void Initialize(NoteData data)
        {
            base.Initialize(data);
            isHolding = false;
            headJudged = false;
            headJudgment = JudgmentType.Miss;
            holdStartTime = 0f;
            lastSampleTime = 0f;
            successProgress = 0f;

            if (tailTransform != null)
            {
                Vector3 tailPos = data.SpawnPosition;
                tailPos.z = spawnZ;
                tailTransform.position = tailPos;
            }
        }

        protected override void UpdatePosition()
        {
            if (headJudged && isHolding)
            {
                UpdateHoldVisuals();
                EvaluateHold();
                CheckHoldEnd();
                return;
            }

            base.UpdatePosition();
            UpdateTailPosition();
            UpdateConnectionLine();
        }

        private void UpdateTailPosition()
        {
            if (tailTransform == null || Data == null) return;

            float tailTimeUntilHit = Data.EndTime - SongTime;
            float tailProgress = 1f - (tailTimeUntilHit / Data.approachTime);
            tailProgress = Mathf.Clamp01(tailProgress);

            Vector3 judgePos = Data.SpawnPosition;
            judgePos.z = judgePlaneZ;

            Vector3 currentTailPos = Vector3.Lerp(Data.SpawnPosition, judgePos, tailProgress);
            tailTransform.position = currentTailPos;

            float zDistance = Mathf.Abs(currentTailPos.z - judgePlaneZ);
            float zRange = Mathf.Abs(spawnZ - judgePlaneZ);
            float scaleFactor = Mathf.Lerp(maxScale, minScale, zDistance / zRange);
            tailTransform.localScale = Vector3.one * scaleFactor;
        }

        private void UpdateConnectionLine()
        {
            if (connectionLine == null) return;
            connectionLine.SetPosition(0, transform.position);
            connectionLine.SetPosition(1, tailTransform != null ? tailTransform.position : transform.position);
        }

        private void UpdateHoldVisuals()
        {
            Vector3 judgePos = Data.SpawnPosition;
            judgePos.z = judgePlaneZ;
            transform.position = judgePos;
            UpdateTailPosition();
            UpdateConnectionLine();

            if (visualTransform != null)
                visualTransform.localScale = Vector3.one * maxScale;
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = maxAlpha;
                spriteRenderer.color = c;
            }
        }

        private void EvaluateHold()
        {
            if (!isHolding) return;
            if (SongTime - lastSampleTime < samplingInterval) return;

            lastSampleTime = SongTime;
            int holdValue = InputManager.Instance.CurrentHoldValue;
            if (holdValue >= Data.threshold)
            {
                successProgress += samplingInterval;
            }
        }

        private void CheckHoldEnd()
        {
            if (!isHolding) return;
            if (SongTime >= Data.EndTime)
            {
                OnCompleted();
            }
        }

        public void TryHitHead()
        {
            if (headJudged || IsMissed) return;

            float timeDiff = SongTime - Data.time;
            if (!JudgeManager.Instance.IsInHitWindow(timeDiff)) return;

            headJudgment = JudgeManager.Instance.Judge(timeDiff);
            headJudged = true;
            isHolding = true;
            holdStartTime = SongTime;
            lastSampleTime = SongTime;
            ScoreManager.Instance.RegisterJudgment(headJudgment);
            ShowJudgmentEffect(headJudgment);
        }

        public void OnRelease()
        {
            if (!isHolding) return;

            if (SongTime < Data.EndTime - JudgeManager.Instance.GoodWindow)
            {
                isHolding = false;
                OnMiss();
            }
            else
            {
                OnCompleted();
            }
        }

        protected override void CheckMiss()
        {
            if (IsJudged || IsMissed || headJudged) return;
            base.CheckMiss();
        }

        private void OnCompleted()
        {
            if (IsJudged || IsMissed) return;
            IsJudged = true;
            isHolding = false;
            ScoreManager.Instance.RegisterJudgment(headJudgment);
            ShowJudgmentEffect(headJudgment);
            DestroyNote();
        }

        public override void OnHit(JudgmentType judgment)
        {
            TryHitHead();
        }
    }
}
