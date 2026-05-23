using System.Collections.Generic;
using UnityEngine;
using MusicGame.Core;
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
        [SerializeField] private float ribbonSampleTime = 0.08f;
        [SerializeField] private float ribbonWidth = 0.46f;
        [SerializeField] private float ribbonAlpha = 0.62f;
        [SerializeField] private float sequenceBaseScale = 0.78f;
        [SerializeField] private int maxGeneratedPieces = 48;

        private readonly List<HoldPiece> holdPieces = new List<HoldPiece>();
        private readonly List<HoldPathNode> pathNodes = new List<HoldPathNode>();
        private readonly List<RibbonSample> ribbonSamples = new List<RibbonSample>();
        private readonly List<Vector3> ribbonVertices = new List<Vector3>();
        private readonly List<Color> ribbonColors = new List<Color>();
        private readonly List<int> ribbonTriangles = new List<int>();

        private MeshFilter ribbonFilter;
        private MeshRenderer ribbonRenderer;
        private Mesh ribbonMesh;
        private bool isHolding;
        private bool headJudged;
        private bool tailJudged;
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
            tailJudged = false;
            headJudgment = JudgmentType.Miss;
            lastSampleTime = 0f;
            successProgress = 0f;

            transform.position = Vector3.zero;
            if (connectionLine != null)
                connectionLine.enabled = false;

            BuildHoldVisuals(data);
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
                TryHitTailSlide();
                CheckHoldEnd();
                return;
            }

            UpdateSequenceVisuals();
        }

        private void EvaluateHold()
        {
            if (!isHolding || InputManager.Instance == null) return;
            if (SongTime - lastSampleTime < samplingInterval) return;

            lastSampleTime = SongTime;
            if (InputManager.Instance.CurrentHoldValue >= Data.threshold)
                successProgress += samplingInterval;
        }

        private void CheckHoldEnd()
        {
            if (!isHolding) return;
            if (SongTime > Data.EndTime + JudgeManager.Instance.MissWindow)
                OnMiss();
        }

        public void TryHitHead()
        {
            if (headJudged || IsMissed || InputManager.Instance == null) return;

            float timeDiff = SongTime - Data.time;
            if (!JudgeManager.Instance.IsInHitWindow(timeDiff)) return;
            if (InputManager.Instance.CurrentHoldValue < Data.threshold) return;

            headJudgment = JudgeManager.Instance.Judge(timeDiff);
            headJudged = true;
            isHolding = true;
            lastSampleTime = SongTime;
        }

        public void OnRelease()
        {
            if (!isHolding) return;

            if (SongTime < Data.EndTime - JudgeManager.Instance.GoodWindow || !tailJudged)
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

        private void TryHitTailSlide()
        {
            if (!isHolding || tailJudged || IsJudged || IsMissed || InputManager.Instance == null) return;

            float timeDiff = SongTime - Data.EndTime;
            if (!JudgeManager.Instance.IsInHitWindow(timeDiff)) return;

            FlickDirection detectedDir = InputManager.Instance.DetectFlickDirection();
            if (detectedDir != Data.flickDirection) return;

            tailJudged = true;
            OnCompleted();
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

        private void BuildHoldVisuals(NoteData data)
        {
            EnsureRibbonRenderer();
            EnsureTemplatePieces();

            foreach (HoldPiece piece in holdPieces)
            {
                piece.Renderer.gameObject.SetActive(false);
            }

            BuildPathNodes(data);
            ClearRibbon();

            int pieceIndex = 0;
            pieceIndex = ConfigurePiece(pieceIndex, "click", data.time, data.SpawnPosition);

            if (data.attentionPoints != null)
            {
                for (int i = 0; i < data.attentionPoints.Count; i++)
                {
                    NotePathPoint point = data.attentionPoints[i];
                    float hitTime = Mathf.Clamp(data.time + point.timeOffset, data.time, data.EndTime);
                    pieceIndex = ConfigurePiece(pieceIndex, "click", hitTime, point.Position);
                }
            }

            ConfigurePiece(pieceIndex, "slide", data.EndTime, data.EndPosition);
            UpdateSequenceVisuals();
        }

        private void EnsureRibbonRenderer()
        {
            if (ribbonFilter != null && ribbonRenderer != null && ribbonMesh != null) return;

            Transform existing = transform.Find("Hold_RibbonMesh");
            GameObject ribbonObject = existing != null ? existing.gameObject : new GameObject("Hold_RibbonMesh");
            ribbonObject.transform.SetParent(transform, false);
            ribbonObject.transform.localPosition = Vector3.zero;
            ribbonObject.transform.localRotation = Quaternion.identity;
            ribbonObject.transform.localScale = Vector3.one;

            ribbonFilter = ribbonObject.GetComponent<MeshFilter>();
            if (ribbonFilter == null)
                ribbonFilter = ribbonObject.AddComponent<MeshFilter>();

            ribbonRenderer = ribbonObject.GetComponent<MeshRenderer>();
            if (ribbonRenderer == null)
                ribbonRenderer = ribbonObject.AddComponent<MeshRenderer>();

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Standard");
            ribbonRenderer.sharedMaterial = new Material(shader) { color = Color.white };
            ribbonRenderer.sortingOrder = 1;

            ribbonMesh = ribbonFilter.sharedMesh;
            if (ribbonMesh == null)
            {
                ribbonMesh = new Mesh { name = "HoldRibbonMesh" };
                ribbonFilter.sharedMesh = ribbonMesh;
            }
        }

        private void BuildPathNodes(NoteData data)
        {
            pathNodes.Clear();
            pathNodes.Add(new HoldPathNode(data.time, data.SpawnPosition));

            if (data.attentionPoints != null)
            {
                for (int i = 0; i < data.attentionPoints.Count; i++)
                {
                    NotePathPoint point = data.attentionPoints[i];
                    float hitTime = Mathf.Clamp(data.time + point.timeOffset, data.time, data.EndTime);
                    pathNodes.Add(new HoldPathNode(hitTime, point.Position));
                }
            }

            pathNodes.Add(new HoldPathNode(data.EndTime, data.EndPosition));
            pathNodes.Sort((left, right) => left.HitTime.CompareTo(right.HitTime));
        }

        private void EnsureTemplatePieces()
        {
            if (spriteRenderer != null && !ContainsRenderer(spriteRenderer))
                holdPieces.Add(new HoldPiece(spriteRenderer, sequenceBaseScale));

            if (tailSpriteRenderer != null && !ContainsRenderer(tailSpriteRenderer))
                holdPieces.Add(new HoldPiece(tailSpriteRenderer, sequenceBaseScale));
        }

        private bool ContainsRenderer(SpriteRenderer renderer)
        {
            for (int i = 0; i < holdPieces.Count; i++)
            {
                if (holdPieces[i].Renderer == renderer) return true;
            }

            return false;
        }

        private int ConfigurePiece(int index, string shape, float hitTime, Vector3 hitPosition)
        {
            if (index >= maxGeneratedPieces + 3) return index;

            HoldPiece piece = GetOrCreatePiece(index);
            Sprite sprite = NoteVisualManager.LoadNoteSprite(NoteVisualManager.GetHoldSpritePath(Data, shape));
            if (sprite != null)
                piece.Renderer.sprite = sprite;

            piece.Shape = shape;
            piece.HitTime = hitTime;
            piece.HitPosition = hitPosition;
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
                GameObject pieceObject = new GameObject("Hold_Marker");
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
                if (progress >= 1f && SongTime > piece.HitTime + JudgeManager.Instance.GoodWindow)
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

            UpdateRibbonVisual();
        }

        private void UpdateRibbonVisual()
        {
            if (ribbonMesh == null || ribbonRenderer == null || Data == null || pathNodes.Count < 2) return;

            BuildVisibleRibbonSamples();
            if (ribbonSamples.Count < 2)
            {
                ClearRibbon();
                return;
            }

            ribbonVertices.Clear();
            ribbonColors.Clear();
            ribbonTriangles.Clear();

            Color directionColor = GetRibbonColor(Data.flickDirection);
            for (int i = 0; i < ribbonSamples.Count; i++)
            {
                RibbonSample sample = ribbonSamples[i];
                Vector2 tangent = GetSampleTangent(i);
                Vector2 normal = new Vector2(-tangent.y, tangent.x);
                float depthScale = GetDepthScale(sample.WorldPosition);
                float halfWidth = ribbonWidth * depthScale * 0.5f;
                Vector3 offset = new Vector3(normal.x * halfWidth, normal.y * halfWidth, 0f);

                ribbonVertices.Add(sample.WorldPosition - offset);
                ribbonVertices.Add(sample.WorldPosition + offset);

                Color color = directionColor;
                color.a = sample.Alpha * ribbonAlpha;
                ribbonColors.Add(color);
                ribbonColors.Add(color);
            }

            for (int i = 0; i < ribbonSamples.Count - 1; i++)
            {
                int current = i * 2;
                int next = current + 2;
                ribbonTriangles.Add(current);
                ribbonTriangles.Add(next);
                ribbonTriangles.Add(current + 1);
                ribbonTriangles.Add(current + 1);
                ribbonTriangles.Add(next);
                ribbonTriangles.Add(next + 1);
            }

            ribbonMesh.Clear();
            ribbonMesh.SetVertices(ribbonVertices);
            ribbonMesh.SetColors(ribbonColors);
            ribbonMesh.SetTriangles(ribbonTriangles, 0);
            ribbonMesh.RecalculateBounds();
            ribbonRenderer.enabled = true;
        }

        private void BuildVisibleRibbonSamples()
        {
            ribbonSamples.Clear();

            float startTime = Data.time;
            float endTime = Data.EndTime;
            float step = Mathf.Max(0.02f, ribbonSampleTime);
            int sampleCount = Mathf.Clamp(Mathf.CeilToInt((endTime - startTime) / step) + 1, 2, maxGeneratedPieces + 2);

            for (int i = 0; i < sampleCount; i++)
            {
                float normalized = i / (sampleCount - 1f);
                float hitTime = Mathf.Lerp(startTime, endTime, normalized);
                float timeUntilHit = hitTime - SongTime;
                float progress = Mathf.Clamp01(1f - (timeUntilHit / Data.approachTime));
                if (progress <= 0f || progress >= 1f)
                    continue;

                Vector3 hitPosition = GetPathPosition(hitTime);
                Vector3 spawnPosition = hitPosition;
                spawnPosition.z = spawnZ;
                Vector3 judgePosition = hitPosition;
                judgePosition.z = judgePlaneZ;
                Vector3 worldPosition = Vector3.Lerp(spawnPosition, judgePosition, progress);

                float zDistance = Mathf.Abs(worldPosition.z - judgePlaneZ);
                float zRange = Mathf.Max(0.001f, Mathf.Abs(spawnZ - judgePlaneZ));
                float depthAlpha = Mathf.Lerp(maxAlpha, minAlpha, zDistance / zRange);
                float edgeFade = Mathf.Clamp01(Mathf.Min(normalized, 1f - normalized) * 5f);
                ribbonSamples.Add(new RibbonSample(worldPosition, depthAlpha * edgeFade));
            }
        }

        private Vector3 GetPathPosition(float hitTime)
        {
            if (pathNodes.Count == 0) return Data.SpawnPosition;
            if (hitTime <= pathNodes[0].HitTime) return pathNodes[0].Position;

            for (int i = 0; i < pathNodes.Count - 1; i++)
            {
                HoldPathNode start = pathNodes[i];
                HoldPathNode end = pathNodes[i + 1];
                if (hitTime > end.HitTime) continue;

                float duration = Mathf.Max(0.001f, end.HitTime - start.HitTime);
                float normalized = Mathf.Clamp01((hitTime - start.HitTime) / duration);
                return Vector3.Lerp(start.Position, end.Position, normalized);
            }

            return pathNodes[pathNodes.Count - 1].Position;
        }

        private Vector2 GetSampleTangent(int index)
        {
            Vector3 previous = ribbonSamples[Mathf.Max(0, index - 1)].WorldPosition;
            Vector3 next = ribbonSamples[Mathf.Min(ribbonSamples.Count - 1, index + 1)].WorldPosition;
            Vector2 tangent = new Vector2(next.x - previous.x, next.y - previous.y);
            return tangent.sqrMagnitude < 0.0001f ? Vector2.right : tangent.normalized;
        }

        private float GetDepthScale(Vector3 worldPosition)
        {
            float zRange = Mathf.Max(0.001f, Mathf.Abs(spawnZ - judgePlaneZ));
            float zDistance = Mathf.Abs(worldPosition.z - judgePlaneZ);
            return Mathf.Lerp(maxScale, minScale, zDistance / zRange);
        }

        private void ClearRibbon()
        {
            if (ribbonMesh != null)
                ribbonMesh.Clear();
            if (ribbonRenderer != null)
                ribbonRenderer.enabled = false;
        }

        private static Color GetRibbonColor(FlickDirection direction)
        {
            return direction switch
            {
                FlickDirection.Right => new Color(0.1f, 0.95f, 1f, 1f),
                FlickDirection.Left => Color.white,
                FlickDirection.Up => new Color(1f, 0.18f, 0.2f, 1f),
                FlickDirection.Down => new Color(0.2f, 0.45f, 1f, 1f),
                _ => Color.white
            };
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
        }

        private struct HoldPathNode
        {
            public HoldPathNode(float hitTime, Vector3 position)
            {
                HitTime = hitTime;
                Position = position;
            }

            public readonly float HitTime;
            public readonly Vector3 Position;
        }

        private struct RibbonSample
        {
            public RibbonSample(Vector3 worldPosition, float alpha)
            {
                WorldPosition = worldPosition;
                Alpha = alpha;
            }

            public readonly Vector3 WorldPosition;
            public readonly float Alpha;
        }
    }
}
