using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FoodSlicer
{
    public class FloatingScoreItem : MonoBehaviour
    {
        [SerializeField] private Text scoreText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float upwardTravelDistance = 90f;
        [SerializeField] private float duration = 0.85f;

        private RectTransform rectTransform;
        private Coroutine animateRoutine;
        private float baseScale = 1f;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (scoreText == null) scoreText = GetComponentInChildren<Text>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Initialize(string text, Color color, float scale = 1f)
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (scoreText == null) scoreText = GetComponentInChildren<Text>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            if (scoreText != null)
            {
                scoreText.text = text;
                scoreText.color = color;
            }

            baseScale = scale;
            transform.localScale = Vector3.one * baseScale;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (animateRoutine != null) StopCoroutine(animateRoutine);
            animateRoutine = StartCoroutine(AnimateRoutine());
        }

        private IEnumerator AnimateRoutine()
        {
            float elapsed = 0f;
            Vector2 startPos = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
            Vector3 targetScale = Vector3.one * baseScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Slight upward floating movement
                if (rectTransform != null)
                {
                    float travel = upwardTravelDistance * Mathf.SmoothStep(0f, 1f, t);
                    rectTransform.anchoredPosition = new Vector2(startPos.x, startPos.y + travel);
                }

                // Scale pop: punch up to ~1.35x in first 25%, then settle
                if (t < 0.25f)
                {
                    float punch = 1f + Mathf.Sin((t / 0.25f) * Mathf.PI * 0.5f) * 0.35f;
                    transform.localScale = targetScale * punch;
                }
                else
                {
                    transform.localScale = targetScale;
                }

                // Smooth fade out in second half (0.45 -> 1.0)
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = t < 0.45f ? 1f : Mathf.Clamp01(1f - ((t - 0.45f) / 0.55f));
                }

                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 0f;

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
            animateRoutine = null;
        }
    }
}
