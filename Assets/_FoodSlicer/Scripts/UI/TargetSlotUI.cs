using UnityEngine;
using UnityEngine.UI;

namespace FoodSlicer
{
    public class TargetSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private Text countText;
        [SerializeField] private GameObject checkmarkObject;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Color States")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color completedColor = new Color(0.2f, 0.9f, 0.4f); // Golden or green check

        [Header("Animation")]
        [SerializeField] private float progressAnimationDuration = 0.20f;

        private FoodType slotFoodType;
        private int currentTargetCount;
        private int requiredTargetCount;
        private bool isConfigured = false;

        public FoodType SlotFoodType => slotFoodType;
        public bool IsConfigured => isConfigured;
        public float ProgressAnimationDuration => progressAnimationDuration;

        private float targetFillAmount = 0f;
        private Coroutine fillCoroutine;
        private Coroutine punchCoroutine;

        public void SetupSlot(FoodType type, Sprite icon, int required)
        {
            slotFoodType = type;
            requiredTargetCount = required;
            currentTargetCount = 0;
            targetFillAmount = 0f;
            isConfigured = true;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.color = icon != null ? Color.white : Color.clear;
                iconImage.transform.localScale = Vector3.one;
            }

            if (checkmarkObject != null)
            {
                checkmarkObject.SetActive(false);
            }

            if (progressFillImage != null)
            {
                progressFillImage.type = Image.Type.Filled;
                progressFillImage.fillMethod = Image.FillMethod.Horizontal;
                progressFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                progressFillImage.fillAmount = 0f;
                progressFillImage.color = normalColor;
            }

            if (countText != null)
            {
                countText.text = $"{currentTargetCount}/{requiredTargetCount}";
            }

            gameObject.SetActive(true);
        }

        public void UpdateProgress(int current)
        {
            currentTargetCount = Mathf.Min(current, requiredTargetCount);
            targetFillAmount = requiredTargetCount > 0 ? Mathf.Clamp01((float)currentTargetCount / requiredTargetCount) : 1f;

            if (countText != null)
            {
                countText.text = $"{currentTargetCount}/{requiredTargetCount}";
            }

            if (fillCoroutine != null) StopCoroutine(fillCoroutine);
            fillCoroutine = StartCoroutine(SmoothFillRoutine());
        }

        private System.Collections.IEnumerator SmoothFillRoutine()
        {
            if (progressFillImage == null) yield break;

            float startFill = progressFillImage.fillAmount;
            float duration = Mathf.Max(0.05f, progressAnimationDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                progressFillImage.fillAmount = Mathf.Lerp(startFill, targetFillAmount, t);
                yield return null;
            }

            progressFillImage.fillAmount = targetFillAmount;

            bool isComplete = currentTargetCount >= requiredTargetCount;
            if (checkmarkObject != null)
            {
                checkmarkObject.SetActive(isComplete);
            }

            progressFillImage.color = isComplete ? completedColor : normalColor;

            if (isComplete && iconImage != null)
            {
                if (punchCoroutine != null) StopCoroutine(punchCoroutine);
                punchCoroutine = StartCoroutine(IconPunchRoutine());
            }
        }

        private System.Collections.IEnumerator IconPunchRoutine()
        {
            if (iconImage == null) yield break;
            Transform t = iconImage.transform;
            Vector3 normalScale = Vector3.one;
            Vector3 punchScale = new Vector3(1.22f, 1.22f, 1.22f);

            float pDuration = 0.20f;
            float elapsed = 0f;

            while (elapsed < pDuration)
            {
                elapsed += Time.deltaTime;
                float pct = Mathf.Clamp01(elapsed / pDuration);
                t.localScale = pct < 0.5f 
                    ? Vector3.Lerp(normalScale, punchScale, pct / 0.5f) 
                    : Vector3.Lerp(punchScale, normalScale, (pct - 0.5f) / 0.5f);
                yield return null;
            }

            t.localScale = normalScale;
            punchCoroutine = null;
        }

        public void SetBarColor(Color color)
        {
            normalColor = color;
            if (progressFillImage != null && (currentTargetCount < requiredTargetCount))
            {
                progressFillImage.color = color;
            }
        }

        public void HideSlot()
        {
            isConfigured = false;
            gameObject.SetActive(false);
        }
    }
}
