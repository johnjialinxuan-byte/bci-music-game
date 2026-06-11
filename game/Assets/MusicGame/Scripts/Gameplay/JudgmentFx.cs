using System.Collections.Generic;
using UnityEngine;
using MusicGame.Core;

namespace MusicGame.Gameplay
{
    /// <summary>
    /// Sprite-based judgment popup (PERFECT / GOOD / MISS) using the pre-rendered
    /// art in Resources/Images/Judgments. Spawns at the judged position, slams in
    /// with an overshoot, floats up and fades out. MISS additionally shakes.
    /// No prefab/canvas needed — safe to call from any note script.
    /// </summary>
    public class JudgmentFx : MonoBehaviour
    {
        private const float Lifetime = 0.72f;
        private const float SlamDuration = 0.10f;
        private const float SettleDuration = 0.12f;
        private const float FadeStart = 0.42f;
        private const float RiseSpeed = 0.55f;

        private static readonly Dictionary<JudgmentType, Sprite> spriteCache = new Dictionary<JudgmentType, Sprite>();

        private SpriteRenderer spriteRenderer;
        private float timer;
        private float baseScale = 1f;
        private bool shake;
        private Vector3 basePosition;

        public static void Show(JudgmentType judgment, Vector3 worldPosition, float scaleMultiplier = 1f)
        {
            Sprite sprite = LoadSprite(judgment);
            if (sprite == null) return;

            GameObject fxObject = new GameObject($"JudgmentFx_{judgment}");
            fxObject.transform.position = worldPosition;

            SpriteRenderer renderer = fxObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 60;
            Color startColor = renderer.color;
            startColor.a = 0f;
            renderer.color = startColor;

            JudgmentFx fx = fxObject.AddComponent<JudgmentFx>();
            fx.spriteRenderer = renderer;
            fx.baseScale = (judgment == JudgmentType.Perfect ? 1.08f : 1f) * scaleMultiplier;
            fx.shake = judgment == JudgmentType.Miss;
            fx.basePosition = worldPosition;
            fxObject.transform.localScale = Vector3.one * (fx.baseScale * 1.55f);
        }

        private static Sprite LoadSprite(JudgmentType judgment)
        {
            if (spriteCache.TryGetValue(judgment, out Sprite cached) && cached != null)
                return cached;

            string spriteName = judgment switch
            {
                JudgmentType.Perfect => "perfect",
                JudgmentType.Good => "good",
                _ => "miss"
            };

            Sprite sprite = Resources.Load<Sprite>($"Images/Judgments/{spriteName}");
            if (sprite != null)
                spriteCache[judgment] = sprite;
            return sprite;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= Lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float scale;
            float alpha = 1f;
            if (timer < SlamDuration)
            {
                // Slam in: oversized → slightly undershot, snapping to full opacity.
                float t = timer / SlamDuration;
                scale = Mathf.Lerp(1.55f, 0.92f, t * t);
                alpha = Mathf.Clamp01(t * 2.5f);
            }
            else if (timer < SlamDuration + SettleDuration)
            {
                // Settle with a small overshoot bump.
                float t = (timer - SlamDuration) / SettleDuration;
                scale = Mathf.Lerp(0.92f, 1f, t) + Mathf.Sin(t * Mathf.PI) * 0.05f;
            }
            else
            {
                scale = 1f;
            }

            if (timer > FadeStart)
                alpha = Mathf.Min(alpha, 1f - (timer - FadeStart) / (Lifetime - FadeStart));

            float rise = timer > SlamDuration ? (timer - SlamDuration) * RiseSpeed : 0f;
            float shakeX = shake && timer < 0.18f
                ? Mathf.Sin(timer * 90f) * 0.07f * (1f - timer / 0.18f)
                : 0f;

            transform.position = basePosition + new Vector3(shakeX, rise, 0f);
            transform.localScale = Vector3.one * (baseScale * scale);

            Color color = spriteRenderer.color;
            color.a = Mathf.Clamp01(alpha);
            spriteRenderer.color = color;
        }
    }
}
