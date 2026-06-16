using UnityEngine;
using MusicGame.Core;
using MusicGame.Gameplay;
using MusicGame.Audio;
using MusicGame.Managers;

namespace MusicGame.Notes
{
    public abstract class NoteBase : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected Transform visualTransform;

        [Header("Pseudo-3D Settings")]
        [SerializeField] protected float judgePlaneZ = 0f;
        [SerializeField] protected float spawnZ = 10f;
        [SerializeField] protected float minScale = 0.22f;
        [SerializeField] protected float maxScale = 0.68f;
        [SerializeField] protected float minAlpha = 0.4f;


        [Header("Judgment SFX")]
        [SerializeField] private string successCueSheet = "cuesheet2";
        [SerializeField] private string successCueName = "";
        [SerializeField] protected float maxAlpha = 1.0f;
        [SerializeField] protected float mobileScaleMultiplier = 0.82f;

        protected NoteData Data { get; private set; }
        protected float SongTime => MusicTime.Current;
        protected bool IsJudged { get; set; }
        protected bool IsMissed { get; set; }

        public float TargetTime => Data.time;
        public NoteType Type => Data.noteType;
        public bool IsActive => gameObject.activeInHierarchy && !IsJudged && !IsMissed;

        protected virtual void Update()
        {
            if (Data == null) return;
            UpdatePosition();
            CheckMiss();
        }

public virtual void Initialize(NoteData data)
        {
            Data = data;
            IsJudged = false;
            IsMissed = false;
            spawnZ = Mathf.Max(spawnZ, 14f);
            minScale = Mathf.Min(minScale, 0.14f);

            Vector3 initialPosition = data.SpawnPosition;
            initialPosition.z = spawnZ;
            transform.position = initialPosition;

            LoadNoteSprite(data);
            ApplyInitialDepthVisual();
        }
        
        private void LoadNoteSprite(NoteData data)
        {
            if (spriteRenderer == null) return;
            
            string spritePath = NoteVisualManager.GetNoteSpritePath(data);
            Sprite sprite = NoteVisualManager.LoadNoteSprite(spritePath);
            
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
                spriteRenderer.color = Color.white;
                Debug.Log($"[NoteBase] Loaded sprite: {spritePath}");
            }
            else
            {
                Debug.LogWarning($"[NoteBase] Could not load sprite: {spritePath}");
            }
        }

protected void ApplyInitialDepthVisual()
        {
            float scaleFactor = minScale;
            if (visualTransform != null)
                visualTransform.localScale = Vector3.one * scaleFactor * PlatformScaleMultiplier;

            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = minAlpha;
                spriteRenderer.color = c;
            }
        }


protected virtual void UpdatePosition()
        {
            float timeUntilHit = Data.time - SongTime;
            float progress = 1f - (timeUntilHit / Data.approachTime);
            progress = Mathf.Clamp01(progress);

            Vector3 spawnPos = Data.SpawnPosition;
            spawnPos.z = spawnZ;
            Vector3 judgePos = Data.SpawnPosition;
            judgePos.z = judgePlaneZ;

            Vector3 currentPos = Vector3.Lerp(spawnPos, judgePos, progress);
            transform.position = currentPos;

            float zDistance = Mathf.Abs(currentPos.z - judgePlaneZ);
            float zRange = Mathf.Max(0.001f, Mathf.Abs(spawnZ - judgePlaneZ));
            float scaleFactor = Mathf.Lerp(maxScale, minScale, zDistance / zRange);

            if (visualTransform != null)
                visualTransform.localScale = Vector3.one * scaleFactor * PlatformScaleMultiplier;

            float alpha = Mathf.Lerp(maxAlpha, minAlpha, zDistance / zRange);
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = alpha;
                spriteRenderer.color = c;
            }
        }

        protected virtual void CheckMiss()
        {
            if (IsJudged || IsMissed) return;

            float timeDiff = SongTime - Data.time;
            if (timeDiff > JudgeManager.Instance.MissWindow)
            {
                IsMissed = true;
                OnMiss();
            }
        }

        // These base paths are only exercised by flick notes — HoldNote overrides
        // both OnHit and CheckMiss and registers its own per-element judgments.
        public virtual void OnHit(JudgmentType judgment)
        {
            if (IsJudged || IsMissed) return;
            IsJudged = true;
            ScoreManager.Instance.RegisterJudgment(NoteCategory.Flick, judgment);
            ShowJudgmentEffect(judgment);
            JudgmentFx.Show(judgment, new Vector3(transform.position.x, transform.position.y, judgePlaneZ));
            DestroyNote();
        }

        protected virtual void OnMiss()
        {
            ScoreManager.Instance.RegisterJudgment(NoteCategory.Flick, JudgmentType.Miss);
            DestroyNote();
        }

        protected virtual void ShowJudgmentEffect(JudgmentType judgment)
        {
            if (judgment == JudgmentType.Miss) return;
            AudioManager.Instance?.PlaySFX(successCueSheet, successCueName);
        }

        protected virtual void DestroyNote()
        {
            gameObject.SetActive(false);
        }

        protected float PlatformScaleMultiplier => Application.isMobilePlatform ? mobileScaleMultiplier : 1f;
    }
}
