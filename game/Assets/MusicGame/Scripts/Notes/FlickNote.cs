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
        [SerializeField, Range(0.1f, 1f)] private float visualScaleMultiplier = 0.7f;

        private const float MissDimAlpha = 0.55f;
        private const float MissDimGray = 0.30f;
        private const float MissLingerSeconds = 0.45f;
        private static Material spriteDefaultMaterial;

        private bool dimmed;
        private float deactivateTime;

        public override void Initialize(NoteData data)
        {
            base.Initialize(data);
            dimmed = false;
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 8);
                Material material = GetSpriteDefaultMaterial();
                if (material != null)
                    spriteRenderer.sharedMaterial = material;
            }

            if (arrowTransform != null)
                arrowTransform.rotation = Quaternion.identity;
        }

        protected override void Update()
        {
            base.Update();
            TryHit();
            if (dimmed && Time.time >= deactivateTime)
                DestroyNote();
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

        protected override void CheckMiss()
        {
            if (IsJudged || IsMissed) return;

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
            ApplyMissDim();
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

        private void ApplyMissDim()
        {
            if (spriteRenderer == null) return;

            Color color = spriteRenderer.color;
            color.r = MissDimGray;
            color.g = MissDimGray;
            color.b = MissDimGray;
            color.a *= MissDimAlpha;
            spriteRenderer.color = color;
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
                name = "FlickNoteSpriteDefaultMaterial"
            };
            return spriteDefaultMaterial;
#else
            return null;
#endif
        }
    }
}
