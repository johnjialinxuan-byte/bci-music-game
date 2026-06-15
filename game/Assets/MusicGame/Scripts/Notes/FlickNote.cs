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
        [SerializeField, Range(0.1f, 1.5f)] private float visualScaleMultiplier = 1.08f;

        // A failed flick grays out and lingers briefly instead of vanishing —
        // darkened RGB reads much stronger than transparency on a dark scene.
        private const float MissDimAlpha = 0.55f;
        private const float MissDimGray = 0.30f;
        private const float MissLingerSeconds = 0.45f;
        private bool dimmed;
        private float deactivateTime;

        public override void Initialize(NoteData data)
        {
            base.Initialize(data);
            dimmed = false;
            minAlpha = Mathf.Max(minAlpha, 0.58f);
            maxAlpha = Mathf.Max(maxAlpha, 1f);
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 8);
            }

            // Slide SVGs are authored already pointing in their color's direction
            // (white←, miku→, red↑, blue↓), so the sprite is never rotated — only
            // reset rotation left over from a previous life in the object pool.
            if (arrowTransform != null)
                arrowTransform.rotation = Quaternion.identity;
        }

        protected override void UpdatePosition()
        {
            base.UpdatePosition();
            if (visualTransform != null)
                visualTransform.localScale *= visualScaleMultiplier;

            if (dimmed && spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.r = MissDimGray;
                color.g = MissDimGray;
                color.b = MissDimGray;
                color.a *= MissDimAlpha;
                spriteRenderer.color = color;
            }
        }


protected override void Update()
        {
            base.Update();
            TryHit();
            if (dimmed && Time.time >= deactivateTime)
                DestroyNote();
        }

        protected override void CheckMiss()
        {
            if (IsJudged || IsMissed) return;

            // The flick hit window (configurable up to 500ms) can exceed the
            // generic miss window — never miss a note that is still hittable.
            float missAfter = Mathf.Max(JudgeManager.Instance.MissWindow, JudgeManager.Instance.FlickGreatWindow);
            if (SongTime - Data.time > missAfter)
            {
                IsMissed = true;
                OnMiss();
            }
        }

        protected override void OnMiss()
        {
            ScoreManager.Instance.RegisterJudgment(NoteCategory.Flick, JudgmentType.Miss);
            dimmed = true;
            deactivateTime = Time.time + MissLingerSeconds;
            JudgmentFx.Show(JudgmentType.Miss, new Vector3(Data.x, Data.y, judgePlaneZ));
        }


public void TryHit()
        {
            if (IsJudged || IsMissed || JudgeManager.Instance == null || InputManager.Instance == null) return;

            float timeDiff = SongTime - Data.time;
            if (!JudgeManager.Instance.IsInFlickHitWindow(timeDiff)) return;

            FlickDirection expectedDirection = GetEffectiveDirection(Data.flickDirection);
            if (!InputManager.Instance.TryConsumeFlick(expectedDirection))
                return;

            JudgmentType judgment = JudgeManager.Instance.JudgeFlick(timeDiff);
            base.OnHit(judgment);
        }

        // External judges (e.g. a click-based judge) must not bypass the
        // direction check; route them through the normal flick evaluation.
        public override void OnHit(JudgmentType judgment)
        {
            TryHit();
        }


        protected override void ShowJudgmentEffect(JudgmentType judgment)
        {
            if (judgment == JudgmentType.Miss) return;
            AudioManager.Instance?.PlaySFX("cuesheet0", "");
        }


        private static FlickDirection GetEffectiveDirection(FlickDirection originalDirection)
        {
            return originalDirection;
        }
}
}
