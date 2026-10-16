using UnityEngine;

namespace FoodSlicer
{
    public class FloatingTextManager : MonoBehaviour
    {
        public static FloatingTextManager Instance { get; private set; }

        [Header("Floating Text Setup")]
        [SerializeField] private GameObject floatingTextPrefab;
        [SerializeField] private Canvas floatingCanvas;

        [Header("Colors")]
        [SerializeField] private Color normalScoreColor = new Color(1f, 0.95f, 0.2f); // Golden yellow
        [SerializeField] private Color perfectScoreColor = new Color(0.2f, 1f, 0.5f); // Neon green
        [SerializeField] private Color comboColor = new Color(0.3f, 0.8f, 1f);        // Cyan

        [Header("Screen Placement & Margins")]
        [Range(0f, 1f)] [SerializeField] private float defaultNormalizedX = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float defaultNormalizedY = 0.55f;
        [SerializeField] private float horizontalRandomOffset = 40f;
        [SerializeField] private float screenMargin = 60f;

        public Canvas FloatingCanvas => GetCanvas();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureCanvasReference();
        }

        private void EnsureCanvasReference()
        {
            if (floatingCanvas == null)
            {
                floatingCanvas = GetComponentInParent<Canvas>();
            }
            if (floatingCanvas == null)
            {
                floatingCanvas = Object.FindObjectOfType<Canvas>();
            }
        }

        private Canvas GetCanvas()
        {
            if (floatingCanvas == null)
            {
                EnsureCanvasReference();
            }
            return floatingCanvas;
        }

        public void ShowScorePopup(Vector3 worldPos, int points, bool isPerfect)
        {
            if (floatingTextPrefab == null) return;

            GameObject obj = SpawnFloatingTextObject();
            if (obj == null) return;

            FloatingScoreItem item = obj.GetComponent<FloatingScoreItem>();
            if (item != null)
            {
                if (isPerfect)
                {
                    item.Initialize($"PERFECT!\n+{points}", perfectScoreColor, 1.25f);
                }
                else
                {
                    item.Initialize($"+{points}", normalScoreColor, 1.0f);
                }
            }
        }

        public void ShowComboPopup(Vector3 worldPos, int multiplier)
        {
            if (floatingTextPrefab == null || multiplier <= 1) return;

            GameObject obj = SpawnFloatingTextObject();
            if (obj == null) return;

            FloatingScoreItem item = obj.GetComponent<FloatingScoreItem>();
            if (item != null)
            {
                item.Initialize($"COMBO x{multiplier}!", comboColor, 1.25f);
            }
        }

        private GameObject SpawnFloatingTextObject()
        {
            Canvas canvas = GetCanvas();
            Transform parentTransform = canvas != null ? canvas.transform : transform;

            GameObject obj = PoolManager.Instance != null
                ? PoolManager.Instance.Spawn(floatingTextPrefab, Vector3.zero, Quaternion.identity, parentTransform)
                : Instantiate(floatingTextPrefab, parentTransform);

            if (obj == null) return null;

            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.SetParent(parentTransform, false);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = CalculateClampedScreenPosition(canvas);
                rt.localScale = Vector3.one;
            }

            return obj;
        }

        private Vector2 CalculateClampedScreenPosition(Canvas canvas)
        {
            RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            float canvasWidth = canvasRect != null ? canvasRect.rect.width : Screen.width;
            float canvasHeight = canvasRect != null ? canvasRect.rect.height : Screen.height;

            // Default spawn near screen center (X = 0.5, Y = 0.55)
            float baseX = (defaultNormalizedX - 0.5f) * canvasWidth;
            float baseY = (defaultNormalizedY - 0.5f) * canvasHeight;

            // Small random horizontal offset
            float randomX = Random.Range(-horizontalRandomOffset, horizontalRandomOffset);
            float targetX = baseX + randomX;
            float targetY = baseY;

            // Clamping inside Safe Area and Canvas bounds
            Rect safeArea = Screen.safeArea;
            float screenW = Mathf.Max(1f, Screen.width);
            float screenH = Mathf.Max(1f, Screen.height);

            float scaleX = canvasWidth / screenW;
            float scaleY = canvasHeight / screenH;

            float minX = (safeArea.xMin * scaleX) - (canvasWidth * 0.5f) + screenMargin;
            float maxX = (safeArea.xMax * scaleX) - (canvasWidth * 0.5f) - screenMargin;
            float minY = (safeArea.yMin * scaleY) - (canvasHeight * 0.5f) + screenMargin;
            float maxY = (safeArea.yMax * scaleY) - (canvasHeight * 0.5f) - screenMargin;

            if (minX > maxX) { float t = minX; minX = maxX; maxX = t; }
            if (minY > maxY) { float t = minY; minY = maxY; maxY = t; }

            targetX = Mathf.Clamp(targetX, minX, maxX);
            targetY = Mathf.Clamp(targetY, minY, maxY);

            return new Vector2(targetX, targetY);
        }
    }
}
