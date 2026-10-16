using System.Collections;
using UnityEngine;

namespace FoodSlicer
{
    public class CameraFeedback : MonoBehaviour
    {
        public static CameraFeedback Instance { get; private set; }

        [Header("Camera Transform Tuning")]
        [SerializeField] [Tooltip("Target field of view for portrait aspect ratio")]
        private float defaultFOV = 50f;

        [Header("Shake Presets")]
        [SerializeField] [Tooltip("Shake strength for a normal slice")]
        private float normalSliceShakeStrength = 0.04f;

        [SerializeField] [Tooltip("Shake strength for a perfect slice")]
        private float perfectSliceShakeStrength = 0.09f;

        [SerializeField] [Tooltip("Shake strength for combo milestone")]
        private float comboMilestoneShakeStrength = 0.12f;

        [SerializeField] [Tooltip("Shake strength for board hit / fail")]
        private float failShakeStrength = 0.2f;

        [SerializeField] [Range(0.05f, 0.5f)] [Tooltip("Shake duration in seconds")]
        private float defaultShakeDuration = 0.12f;

        [SerializeField] [Range(5f, 60f)] [Tooltip("Shake oscillation frequency")]
        private float shakeFrequency = 35f;

        private Camera cam;
        private Vector3 initialLocalPos;
        private Coroutine shakeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = GetComponent<Camera>();

            if (cam != null)
            {
                cam.fieldOfView = defaultFOV;
            }

            initialLocalPos = transform.localPosition;
        }

        public void PlaySliceShake()
        {
            TriggerShake(normalSliceShakeStrength, defaultShakeDuration);
        }

        public void PlayPerfectShake()
        {
            TriggerShake(perfectSliceShakeStrength, defaultShakeDuration * 1.3f);
        }

        public void PlayComboShake()
        {
            TriggerShake(comboMilestoneShakeStrength, defaultShakeDuration * 1.5f);
        }

        public void PlayBoardHitShake()
        {
            TriggerShake(failShakeStrength, defaultShakeDuration * 1.8f);
        }

        public void TriggerShake(float strength, float duration)
        {
            if (!Application.isPlaying) return;
            if (!isActiveAndEnabled) return;

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }
            shakeCoroutine = StartCoroutine(ShakeRoutine(strength, duration));
        }

        private IEnumerator ShakeRoutine(float strength, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damp = 1f - Mathf.Clamp01(elapsed / duration);

                float offsetX = Mathf.Sin(elapsed * shakeFrequency) * strength * damp;
                float offsetY = Mathf.Cos(elapsed * shakeFrequency * 1.3f) * strength * damp * 0.7f;

                transform.localPosition = initialLocalPos + new Vector3(offsetX, offsetY, 0f);
                yield return null;
            }

            transform.localPosition = initialLocalPos;
            shakeCoroutine = null;
        }

        public void SetFOV(float fov)
        {
            if (cam != null)
            {
                cam.fieldOfView = fov;
            }
        }
    }
}
