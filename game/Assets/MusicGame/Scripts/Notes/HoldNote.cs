using System.Collections.Generic;
using UnityEngine;
using MusicGame.Core;
using MusicGame.Gameplay;
using MusicGame.Managers;

namespace MusicGame.Notes
{
    public class HoldNote : NoteBase
    {
        [Header("Hold Visuals")]
        [SerializeField] private SpriteRenderer tailSpriteRenderer;
        [SerializeField] private Transform tailTransform;
        [SerializeField] private LineRenderer connectionLine;
        [SerializeField] private float ribbonSampleTime = 0.08f;
        [SerializeField] private float ribbonWidth = 0.38f;
        [SerializeField] private float ribbonAlpha = 0.22f;
        [SerializeField] private float sequenceBaseScale = 0.68f;
        [SerializeField] private float visualFillScaleMultiplier = 0.92f;
        [SerializeField] private float visualFillAlphaMultiplier = 0.88f;

        [SerializeField, Range(0.1f, 1f)] private float tailFlickScaleMultiplier = 0.7f;
        [SerializeField] private int maxGeneratedPieces = 48;

        // Once any of the hold's judgments fails the whole trail grays out:
        // RGB crushed to dark gray plus reduced alpha — far more readable on a
        // dark background than an alpha-only fade.
        private const float DimAlpha = 0.55f;
        private const float DimGray = 0.30f;
        private static Material spriteDefaultMaterial;

        private readonly List<HoldPiece> holdPieces = new List<HoldPiece>();
        private readonly List<HoldPiece> visualFillPieces = new List<HoldPiece>();
        private readonly List<HoldPathNode> pathNodes = new List<HoldPathNode>();
        private readonly List<RibbonSample> ribbonSamples = new List<RibbonSample>();
        private readonly List<Vector3> ribbonVertices = new List<Vector3>();
        private readonly List<Color> ribbonColors = new List<Color>();
        private readonly List<int> ribbonTriangles = new List<int>();

        private MeshFilter ribbonFilter;
        private MeshRenderer ribbonRenderer;
        private Mesh ribbonMesh;
        
        private bool holdJudged;
        private bool isHolding;
        private bool headJudged;
        private bool tailJudged;
        private JudgmentType headJudgment;

        // Per-element judgment state: the head and each checkpoint click are
        // judged at their own moment; rounds inherit the verdict of the click
        // that precedes them (segmentSuccess).
        private readonly List<float> checkpointTimes = new List<float>();
        private bool headResolved;
        private bool segmentSuccess;
        private int nextCheckpointIndex;
        private int nextRoundIndex;
        private int configuredFillCount;
        private bool holdDimmed;

        public float EndTime => Data.EndTime;
        public bool IsHolding => isHolding;

        public override void Initialize(NoteData data)
        {
            base.Initialize(data);
            isHolding = false;
            holdJudged = false;
            headJudged = false;
            tailJudged = false;
            headJudgment = JudgmentType.Miss;
            headResolved = false;
            segmentSuccess = false;
            nextCheckpointIndex = 0;
            nextRoundIndex = 0;
            holdDimmed = false;

            checkpointTimes.Clear();
            if (data.attentionPoints != null)
            {
                foreach (NotePathPoint point in data.attentionPoints)
                    checkpointTimes.Add(Mathf.Clamp(data.time + point.timeOffset, data.time, data.EndTime));
            }
            checkpointTimes.Sort();

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
            UpdateSequenceVisuals();
            EvaluateTimeline();
            TryHitTailSlide();
            CheckHoldEnd();
        }

        // Walks head / checkpoint clicks / rounds in time order. Clicks are the
        // only judgment points; the rounds between click N and click N+1 all
        // score iff click N succeeded. Any failure dims the rest of the hold.
        private void EvaluateTimeline()
        {
            if (holdJudged || JudgeManager.Instance == null || ScoreManager.Instance == null) return;

            if (!headResolved && SongTime > Data.time + JudgeManager.Instance.GoodWindow)
            {
                headResolved = true;
                segmentSuccess = false;
                holdDimmed = true;
                ApplyHoldDimVisuals();
                ScoreManager.Instance.RegisterJudgment(HeadCategory, JudgmentType.Miss);
                JudgmentFx.Show(JudgmentType.Miss, new Vector3(Data.x, Data.y, judgePlaneZ),
                    Data.isRoundNote ? 0.55f : 1f);
            }

            while (true)
            {
                bool hasCheckpoint = nextCheckpointIndex < checkpointTimes.Count
                    && SongTime >= checkpointTimes[nextCheckpointIndex];
                bool hasRound = headResolved
                    && nextRoundIndex < configuredFillCount
                    && nextRoundIndex < visualFillPieces.Count
                    && SongTime >= visualFillPieces[nextRoundIndex].HitTime;

                if (hasRound && (!hasCheckpoint || visualFillPieces[nextRoundIndex].HitTime <= checkpointTimes[nextCheckpointIndex]))
                {
                    HoldPiece round = visualFillPieces[nextRoundIndex];
                    ScoreManager.Instance.RegisterJudgment(NoteCategory.Round,
                        segmentSuccess ? JudgmentType.Perfect : JudgmentType.Miss);
                    if (segmentSuccess && round.Renderer != null)
                        round.Renderer.gameObject.SetActive(false);
                    nextRoundIndex++;
                    continue;
                }

                if (hasCheckpoint)
                {
                    float checkpointTime = checkpointTimes[nextCheckpointIndex];
                    bool success = isHolding
                        && InputManager.Instance != null
                        && InputManager.Instance.CurrentHoldValue >= Data.threshold;
                    ScoreManager.Instance.RegisterJudgment(NoteCategory.Click,
                        success ? JudgmentType.Perfect : JudgmentType.Miss);
                    segmentSuccess = success;
                    Vector3 pathPosition = GetPathPosition(checkpointTime);
                    if (success)
                    {
                        HideClickPieceAt(checkpointTime);
                        JudgmentFx.Show(JudgmentType.Perfect, new Vector3(pathPosition.x, pathPosition.y, judgePlaneZ), 0.7f);
                    }
                    else
                    {
                        holdDimmed = true;
                        ApplyHoldDimVisuals();
                        JudgmentFx.Show(JudgmentType.Miss, new Vector3(pathPosition.x, pathPosition.y, judgePlaneZ), 0.7f);
                    }
                    nextCheckpointIndex++;
                    continue;
                }

                break;
            }
        }

        private NoteCategory HeadCategory => Data.isRoundNote ? NoteCategory.Round : NoteCategory.Click;

        private void HideClickPieceAt(float hitTime)
        {
            foreach (HoldPiece piece in holdPieces)
            {
                if (piece.Renderer == null || piece.Shape != "click") continue;
                if (Mathf.Abs(piece.HitTime - hitTime) < 0.001f)
                    piece.Renderer.gameObject.SetActive(false);
            }
        }

        private void CheckHoldEnd()
        {
            // Short holds can have time+GoodWindow beyond EndTime; the head must
            // stay catchable for its full window before the hold finalizes.
            float finishTime = Mathf.Max(Data.EndTime, Data.time + JudgeManager.Instance.GoodWindow);
            if (!holdJudged && SongTime > finishTime)
            {
                EvaluateTimeline();
                holdJudged = true;
                isHolding = false;
            }

            if (Data.HasTailFlick && !tailJudged && SongTime > Data.EndTime + JudgeManager.Instance.MissWindow)
                ResolveTailJudgment(JudgmentType.Miss);

            TryFinishJudgment();
        }

        public void TryHitHead()
        {
            if (headJudged || holdJudged || IsJudged || InputManager.Instance == null) return;

            float timeDiff = SongTime - Data.time;
            if (Mathf.Abs(timeDiff) > JudgeManager.Instance.GoodWindow) return;
            if (InputManager.Instance.CurrentHoldValue < Data.threshold) return;

            // Hold elements are binary: reaching here means the head was caught
            // inside the window with attention over threshold, so it is a Perfect.
            // (No Good tier for holds — see also checkpoint/tail judgments.)
            headJudgment = JudgmentType.Perfect;
            headJudged = true;
            headResolved = true;
            segmentSuccess = true;
            isHolding = true;
            ScoreManager.Instance.RegisterJudgment(HeadCategory, headJudgment);
            // The hold's success SFX plays the moment the head is caught.
            ShowJudgmentEffect(headJudgment);
            // Round notes use a smaller popup so dense charts don't flood the view.
            JudgmentFx.Show(headJudgment, new Vector3(Data.x, Data.y, judgePlaneZ),
                Data.isRoundNote ? 0.55f : 1f);
        }

        protected override void CheckMiss()
        {
            if (IsJudged) return;
            CheckHoldEnd();
        }

private void TryHitTailSlide()
        {
            if (!Data.HasTailFlick || tailJudged || IsJudged || InputManager.Instance == null || JudgeManager.Instance == null) return;

            float timeDiff = SongTime - Data.EndTime;
            if (!JudgeManager.Instance.IsInFlickHitWindow(timeDiff)) return;

            FlickDirection expectedDirection = GetEffectiveTailFlickDirection(Data.flickDirection);
            if (!InputManager.Instance.TryConsumeFlick(expectedDirection)) return;

            // Binary like the rest of the hold: a correctly-aimed in-window flick
            // is a Perfect, otherwise it never reaches here (miss handled on timeout).
            ResolveTailJudgment(JudgmentType.Perfect);
            TryFinishJudgment();
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

            foreach (HoldPiece piece in visualFillPieces)
            {
                piece.Renderer.gameObject.SetActive(false);
            }

            BuildPathNodes(data);
            BuildVisualFillPieces(data);
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

            if (data.HasTailFlick)
                ConfigurePiece(pieceIndex, "slide", data.EndTime, data.EndPosition);
            UpdateSequenceVisuals();
        }

        private void BuildVisualFillPieces(NoteData data)
        {
            Sprite fillSprite = NoteVisualManager.LoadNoteSprite(NoteVisualManager.GetHoldSpritePath(data, "round"));
            // Spacing/cap shared with ScoreManager via HoldScoring so scored
            // round count always matches the dots actually built here.
            float interval = HoldScoring.RoundInterval;
            int fillIndex = 0;

            for (int segment = 0; segment < pathNodes.Count - 1 && fillIndex < HoldScoring.MaxRounds; segment++)
            {
                HoldPathNode start = pathNodes[segment];
                HoldPathNode end = pathNodes[segment + 1];
                float duration = Mathf.Max(0f, end.HitTime - start.HitTime);
                int count = Mathf.Max(0, Mathf.CeilToInt(duration / interval) - 1);

                for (int i = 1; i <= count && fillIndex < HoldScoring.MaxRounds; i++)
                {
                    float normalized = i / (count + 1f);
                    HoldPiece piece = GetOrCreateVisualFillPiece(fillIndex);
                    if (fillSprite != null)
                        piece.Renderer.sprite = fillSprite;

                    ApplySpriteMaterial(piece.Renderer);
                    piece.Shape = "round";
                    piece.HitTime = Mathf.Lerp(start.HitTime, end.HitTime, normalized);
                    piece.HitPosition = Vector3.Lerp(start.Position, end.Position, normalized);
                    // Reset pooled renderer color — see ConfigurePiece.
                    piece.Renderer.color = Color.white;
                    piece.Renderer.sortingOrder = 2;
                    piece.Renderer.gameObject.name = $"Hold_VisualRound_{fillIndex:000}";
                    piece.Renderer.gameObject.SetActive(true);
                    fillIndex++;
                }
            }

            configuredFillCount = fillIndex;
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
            {
                ApplySpriteMaterial(spriteRenderer);
                holdPieces.Add(new HoldPiece(spriteRenderer, sequenceBaseScale));
            }

            if (tailSpriteRenderer != null && !ContainsRenderer(tailSpriteRenderer))
            {
                ApplySpriteMaterial(tailSpriteRenderer);
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

        private int ConfigurePiece(int index, string shape, float hitTime, Vector3 hitPosition)
        {
            if (index >= maxGeneratedPieces + 3) return index;

            HoldPiece piece = GetOrCreatePiece(index);
            Sprite sprite = NoteVisualManager.LoadNoteSprite(NoteVisualManager.GetHoldSpritePath(Data, shape));
            if (sprite != null)
                piece.Renderer.sprite = sprite;

            ApplySpriteMaterial(piece.Renderer);
            piece.Shape = shape;
            piece.HitTime = hitTime;
            piece.HitPosition = hitPosition;
            // Pieces are pooled with the note: a previous life may have grayed this
            // renderer out (miss dim), so the color must be reset on every build.
            piece.Renderer.color = Color.white;
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
                ApplySpriteMaterial(renderer);
                renderer.color = Color.white;
                holdPieces.Add(new HoldPiece(renderer, sequenceBaseScale));
            }

            return holdPieces[index];
        }

        private HoldPiece GetOrCreateVisualFillPiece(int index)
        {
            while (visualFillPieces.Count <= index)
            {
                GameObject pieceObject = new GameObject("Hold_VisualRound");
                pieceObject.transform.SetParent(transform, false);
                SpriteRenderer renderer = pieceObject.AddComponent<SpriteRenderer>();
                ApplySpriteMaterial(renderer);
                renderer.color = Color.white;
                visualFillPieces.Add(new HoldPiece(renderer, sequenceBaseScale));
            }

            return visualFillPieces[index];
        }

        private void UpdateSequenceVisuals()
        {
            float zRange = Mathf.Max(0.001f, Mathf.Abs(spawnZ - judgePlaneZ));

            UpdateVisualFillPieces(zRange);

            foreach (HoldPiece piece in holdPieces)
            {
                if (piece.Renderer == null || !piece.Renderer.gameObject.activeSelf) continue;

                float timeUntilHit = piece.HitTime - SongTime;
                float progress = Mathf.Clamp01(1f - (timeUntilHit / Data.approachTime));
                float scaleMultiplier = piece.Shape == "slide" ? tailFlickScaleMultiplier : 1f;

                // The head icon disappears right after its own hit window (or as
                // soon as it is caught) — the ribbon/fill keeps showing the hold
                // body, and late catches stay possible even without the icon.
                // The tail slide stays for its whole flick window.
                bool isHeadPiece = piece.Shape == "click" && Mathf.Approximately(piece.HitTime, Data.time);
                bool isTailPiece = piece.Shape == "slide";
                bool expired;
                if (isHeadPiece)
                    expired = headJudged || SongTime > piece.HitTime + JudgeManager.Instance.GoodWindow;
                else if (isTailPiece)
                    expired = tailJudged || SongTime > piece.HitTime + JudgeManager.Instance.FlickGreatWindow;
                else
                    expired = SongTime > piece.HitTime + JudgeManager.Instance.GoodWindow;

                if (progress >= 1f && expired)
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
                float scaleFactor = Mathf.Lerp(maxScale, minScale, zDistance / zRange)
                    * piece.BaseScale * scaleMultiplier * PlatformScaleMultiplier;
                piece.Renderer.transform.localScale = Vector3.one * scaleFactor;

                Color color = piece.Renderer.color;
                if (holdDimmed)
                {
                    color.r = DimGray;
                    color.g = DimGray;
                    color.b = DimGray;
                }
                color.a = Mathf.Lerp(maxAlpha, minAlpha, zDistance / zRange) * (holdDimmed ? DimAlpha : 1f);
                piece.Renderer.color = color;
            }

            UpdateRibbonVisual();
        }

        // Visual filler circles build the Hold body; they never participate in judgment.
        private void UpdateVisualFillPieces(float zRange)
        {
            foreach (HoldPiece piece in visualFillPieces)
            {
                if (piece.Renderer == null || !piece.Renderer.gameObject.activeSelf) continue;

                float progress = Mathf.Clamp01(1f - ((piece.HitTime - SongTime) / Data.approachTime));
                if (progress >= 1f)
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
                float scaleFactor = Mathf.Lerp(maxScale, minScale, zDistance / zRange)
                    * piece.BaseScale * visualFillScaleMultiplier * PlatformScaleMultiplier;
                piece.Renderer.transform.localScale = Vector3.one * scaleFactor;

                Color color = piece.Renderer.color;
                if (holdDimmed)
                {
                    color.r = DimGray;
                    color.g = DimGray;
                    color.b = DimGray;
                }
                color.a = Mathf.Lerp(maxAlpha, minAlpha, zDistance / zRange) * visualFillAlphaMultiplier
                    * (holdDimmed ? DimAlpha : 1f);
                piece.Renderer.color = color;
            }
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

                Color color = holdDimmed
                    ? new Color(DimGray, DimGray, DimGray, 1f)
                    : directionColor;
                color.a = sample.Alpha * ribbonAlpha * (holdDimmed ? DimAlpha : 1f);
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
                ribbonSamples.Add(new RibbonSample(worldPosition, depthAlpha));
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
            return Mathf.Lerp(maxScale, minScale, zDistance / zRange) * PlatformScaleMultiplier;
        }

        private static void ApplySpriteMaterial(SpriteRenderer renderer)
        {
            if (renderer == null) return;

            Material material = GetSpriteDefaultMaterial();
            if (material != null)
                renderer.sharedMaterial = material;
        }

        private static Material GetSpriteDefaultMaterial()
        {
            // Android only: force Sprites/Default so the SVG sprites render there.
            // On desktop/iOS keep the prefab's original material — forcing this
            // material regressed the miss-dim tint (the pre-pull behavior worked).
#if UNITY_ANDROID && !UNITY_EDITOR
            if (spriteDefaultMaterial != null)
                return spriteDefaultMaterial;

            // URP project: an unlit URP sprite shader renders without 2D lights AND
            // tints via SpriteRenderer.color (so the miss-dim works). Fall back to
            // the built-in sprite shader only if the URP one isn't in the build.
            Shader spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                ?? Shader.Find("Sprites/Default");
            if (spriteShader == null)
                return null;

            spriteDefaultMaterial = new Material(spriteShader)
            {
                name = "HoldNoteSpriteDefaultMaterial"
            };
            return spriteDefaultMaterial;
#else
            return null;
#endif
        }

        private void ApplyHoldDimVisuals()
        {
            ApplyDimToPieces(holdPieces);
            ApplyDimToPieces(visualFillPieces);
        }

        private static void ApplyDimToPieces(List<HoldPiece> pieces)
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                SpriteRenderer renderer = pieces[i].Renderer;
                if (renderer == null || !renderer.gameObject.activeSelf) continue;

                Color color = renderer.color;
                color.r = DimGray;
                color.g = DimGray;
                color.b = DimGray;
                color.a *= DimAlpha;
                renderer.color = color;
            }
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
            // Slide SVGs are authored already pointing in their color's direction
            // (white←, miku→, red↑, blue↓); pieces are never rotated. Resetting
            // still matters because pooled pieces may carry an old rotation.
            pieceTransform.rotation = Quaternion.identity;
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
    

        protected override void ShowJudgmentEffect(JudgmentType judgment)
        {
            if (judgment == JudgmentType.Miss) return;
            MusicGame.Audio.AudioManager.Instance?.PlaySFX("cuesheet1", "");
        }


        private void ResolveTailJudgment(JudgmentType judgment)
        {
            if (tailJudged) return;

            tailJudged = true;
            ScoreManager.Instance.RegisterJudgment(NoteCategory.Flick, judgment);
            Vector3 endPosition = Data.EndPosition;
            if (judgment != JudgmentType.Miss)
            {
                MusicGame.Audio.AudioManager.Instance?.PlaySFX("cuesheet0", "");
            }
            else
            {
                holdDimmed = true;
                ApplyHoldDimVisuals();
            }
            JudgmentFx.Show(judgment, new Vector3(endPosition.x, endPosition.y, judgePlaneZ));
        }

        private void TryFinishJudgment()
        {
            if (!holdJudged || (Data.HasTailFlick && !tailJudged)) return;

            IsJudged = true;
            DestroyNote();
        }


        private static FlickDirection GetEffectiveTailFlickDirection(FlickDirection originalDirection)
        {
            return originalDirection;
        }
}
}
