using UnityEngine;

namespace FoodSlicer
{
    public class HapticManager : MonoBehaviour
    {
        public static HapticManager Instance { get; private set; }

        [Header("Haptic Settings")]
        [SerializeField] [Tooltip("Globally enable or disable haptic vibrations")]
        private bool hapticsEnabled = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (SaveManager.Instance != null)
            {
                hapticsEnabled = SaveManager.Instance.IsHapticsEnabled();
            }
        }

        public void SetHapticsEnabled(bool enabled)
        {
            hapticsEnabled = enabled;
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SetHapticsEnabled(enabled);
            }
        }

        public bool IsHapticsEnabled() => hapticsEnabled;

        public void TriggerLightHaptic()
        {
            if (!hapticsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        public void TriggerMediumHaptic()
        {
            if (!hapticsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        public void TriggerHeavyHaptic()
        {
            if (!hapticsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        public void TriggerSlice() => TriggerLightHaptic();
        public void TriggerPerfectSlice() => TriggerMediumHaptic();
        public void TriggerComboMilestone() => TriggerMediumHaptic();
        public void TriggerBoardHit() => TriggerHeavyHaptic();
        public void TriggerLevelComplete() => TriggerHeavyHaptic();
    }
}
