using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;

namespace MusicGame.Gameplay
{
    public class JudgmentPopup : MonoBehaviour
    {
        [SerializeField] private Text judgmentText;
        [SerializeField] private float floatSpeed = 50f;
        [SerializeField] private float lifetime = 0.5f;

        private float timer;
        private Color originalColor;

        private void Awake()
        {
            if (judgmentText != null)
                originalColor = judgmentText.color;
        }

        public void Initialize(JudgmentType judgment, Vector3 worldPos)
        {
            if (judgmentText == null) return;

            judgmentText.text = judgment.ToString();
            judgmentText.color = GetJudgmentColor(judgment);
            originalColor = judgmentText.color;

            transform.position = worldPos;
            timer = 0f;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += Vector3.up * floatSpeed * Time.deltaTime;

            Color c = originalColor;
            c.a = Mathf.Lerp(1f, 0f, timer / lifetime);
            if (judgmentText != null)
                judgmentText.color = c;
        }

        private Color GetJudgmentColor(JudgmentType judgment)
        {
            return judgment switch
            {
                JudgmentType.Perfect => new Color(1f, 0.85f, 0.2f),
                JudgmentType.Good => new Color(0.2f, 0.6f, 1f),
                JudgmentType.Miss => new Color(0.6f, 0.6f, 0.6f),
                _ => Color.white
            };
        }
    }
}
