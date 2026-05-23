using System.Collections.Generic;
using UnityEngine;
using MusicGame.Core;
using MusicGame.Managers;
using MusicGame.Audio;

namespace MusicGame.Notes
{
    public class HoldNote : NoteBase
    {
        [Header("Hold Visuals")]
        [SerializeField] private SpriteRenderer tailSpriteRenderer;
        [SerializeField] private Transform tailTransform;
        [SerializeField] private LineRenderer connectionLine;
        [SerializeField] private float samplingInterval = 0.1f;
        [SerializeField] private float sequenceStepTime = 0.16f;
        [SerializeField] private float sequenceBaseScale = 0.78f;
        [SerializeField] private int maxGeneratedPieces = 48;

        private readonly List<HoldPiece> holdPieces = new List<HoldPiece>();
        private bool isHolding;
        private bool headJudged;
        private JudgmentType headJudgment;
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
            lastSampleTime = 0f;
            successProgress = 0f;

            transform.position = Vector3.zero;
            if (connectionLine != null)
                connectionLine.enabled = false;

            BuildHoldSequence(data);
        }

        protected override void Update()
        {
            base.Update();
            TryHitHead();
        }

        protected override void UpdatePosition()
        {
            if (headJudged && isHolding)
            {
                UpdateSequenceVisuals();
                EvaluateHold();
                CheckHoldEnd();
                return;
            }

            UpdateSequenceVisuals();
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

            int holdValue = InputManager.Instance.CurrentHoldValue;
            if (holdValue < Data.threshold) return;

            headJudgment = JudgeManager.Instance.Judge(timeDiff);
            headJudged = true;
            isHolding = true;
            lastSampleTime = SongTime;

            AudioManager.Instance?.PlaySFX("cuesheet1", "");
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
            float requiredProgress = Mathf.Max(samplingInterval, Data.duration * 0.6f);
            JudgmentType finalJudgment = successProgress >= requiredProgress ? headJudgment : JudgmentType.Miss;
            ScoreManager.Instance.RegisterJudgment(finalJudgment);
            ShowJudgmentEffect(finalJudgment);
            DestroyNote();
        }

        public override void OnHit(JudgmentType judgment)
        {
            TryHitHead();
        }

        private void BuildHoldSequence(NoteData data)
        {
            EnsureTemplatePieces();

            foreach (HoldPiece piece in holdPieces)
            {
                piece.Renderer.gameObject.SetActive(false);
            }

            int pieceIndex = 0;
            pieceIndex = ConfigurePiece(pieceIndex, "click", data.time, data.SpawnPosition, true);

            float duration = Mathf.Max(data.duration, sequenceStepTime);
            int roundCount = Mathf.Clamp(Mathf.CeilToInt(duration / Mathf.Max(0.05f, sequenceStepTime)) - 1, 1, maxGeneratedPieces);
            for (int i = 0; i < roundCount; i++)
            {
                float normalized = (i + 1f) / (roundCount + 1f);
                float hitTime = Mathf.Lerp(data.time, data.EndTime, normalized);
                Vector3 hitPosition = Vector3.Lerp(data.SpawnPosition, data.EndPosition, normalized);
                pieceIndex = ConfigurePiece(pieceIndex, "round", hitTime, hitPosition, true);
            }

            if (data.attentionPoints != null)
            {
                for (int i = 0; i < data.attentionPoints.Count; i++)
                {
                    NotePathPoint point = data.attentionPoints[i];
                    float hitTime = data.time + point.timeOffset;
                    pieceIndex = ConfigurePiece(pieceIndex, "click", hitTime, point.Position, true);
                }
            }

            pieceIndex = ConfigurePiece(pieceIndex, "slide", data.EndTime, data.EndPosition, true);
            UpdateSequenceVisuals();
        }

        private void EnsureTemplatePieces()
        {
            if (spriteRenderer != null && !ContainsRenderer(spriteRenderer))
            {
                holdPieces.Add(new HoldPiece(spriteRenderer, sequenceBaseScale));
            }

            if (tailSpriteRenderer != null && !ContainsRenderer(tailSpriteRenderer))
            {
                holdPieces.Add(new HoldPiece(tailSpriteRenderer, sequenceBaseScale));
            }
        }

        private bool ContainsRenderer(SpriteRenderer renderer)
        {
            for (int i = 0; i < holdPieces.Count; i++)
            {
                if (holdPieces[i].Renderer == renderer) return true;
            }

            return false;
        }

        private int ConfigurePiece(int index, string shape, float hitTime, Vector3 hitPosition, bool disappearAtPlane)
        {
            if (index >= maxGeneratedPieces + 3) return index;

            HoldPiece piece = GetOrCreatePiece(index);
            Sprite sprite = NoteVisualManager.LoadNoteSprite(NoteVisualManager.GetHoldSpritePath(Data, shape));
            if (sprite != null)
            {
                piece.Renderer.sprite = sprite;
            }

            piece.Shape = shape;
            piece.HitTime = hitTime;
            piece.HitPosition = hitPosition;
            piece.DisappearAtPlane = disappearAtPlane;
            piece.Renderer.sortingOrder = shape == "click" ? 4 : shape == "slide" ? 3 : 2;
            piece.Renderer.gameObject.name = $"Hold_{shape}_{index:00}";
            piece.Renderer.gameObject.SetActive(true);
            SetPieceRotation(piece.Renderer.transform, shape, Data.flickDirection);
            return index + 1;
        }

        private HoldPiece GetOrCreatePiece(int index)
        {
            while (holdPieces.Count <= index)
            {
                GameObject pieceObject = new GameObject("Hold_round");
                pieceObject.transform.SetParent(transform, false);
                SpriteRenderer renderer = pieceObject.AddComponent<SpriteRenderer>();
                renderer.color = Color.white;
                holdPieces.Add(new HoldPiece(renderer, sequenceBaseScale));
            }

            return holdPieces[index];
        }

        private void UpdateSequenceVisuals()
        {
            float zRange = Mathf.Max(0.001f, Mathf.Abs(spawnZ - judgePlaneZ));

            foreach (HoldPiece piece in holdPieces)
            {
                if (piece.Renderer == null || !piece.Renderer.gameObject.activeSelf) continue;

                float timeUntilHit = piece.HitTime - SongTime;
                float progress = Mathf.Clamp01(1f - (timeUntilHit / Data.approachTime));
                if (piece.DisappearAtPlane && progress >= 1f)
                {
                    piece.Renderer.gameObject.SetActive(false);
                    continue;
                }

                Vector3 spawnPosition = piece.HitPosition;
                spawnPosition.z = spawnZ;
                Vector3 judgePosition = piece.HitPosition;
                judgePosition.z = judgePlaneZ;
                Vector3 currentPosition = Vector3.Lerp(spawnPosition, judgePosition, progress);
                piece.Renderer.transform.position = currentPosition;

                float zDistance = Mathf.Abs(currentPosition.z - judgePlaneZ);
                float scaleFactor = Mathf.Lerp(maxScale, minScale, zDistance / zRange) * piece.BaseScale;
                piece.Renderer.transform.localScale = Vector3.one * scaleFactor;

                Color color = piece.Renderer.color;
                color.a = Mathf.Lerp(maxAlpha, minAlpha, zDistance / zRange);
                piece.Renderer.color = color;
            }
        }

        private void SetPieceRotation(Transform pieceTransform, string shape, FlickDirection direction)
        {
            if (shape != "slide")
            {
                pieceTransform.rotation = Quaternion.identity;
                return;
            }

            float angle = direction switch
            {
                FlickDirection.Left => 180f,
                FlickDirection.Right => 0f,
                FlickDirection.Up => 90f,
                FlickDirection.Down => -90f,
                _ => 0f
            };
            pieceTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private sealed class HoldPiece
        {
            public HoldPiece(SpriteRenderer renderer, float baseScale)
            {
                Renderer = renderer;
                BaseScale = baseScale;
            }

            public readonly SpriteRenderer Renderer;
            public readonly float BaseScale;
            public string Shape;
            public float HitTime;
            public Vector3 HitPosition;
            public bool DisappearAtPlane;
        }
    }
}
