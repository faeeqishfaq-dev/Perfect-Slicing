using System;
using UnityEngine;

namespace FoodSlicer
{
    public class ComboManager : MonoBehaviour
    {
        public static ComboManager Instance { get; private set; }

        [Header("Multiplier Settings")]
        [SerializeField] [Range(1, 10)] [Tooltip("Maximum combo multiplier achievable")]
        private int maxMultiplier = 5;

        [SerializeField] [Tooltip("Number of consecutive slices required per multiplier step")]
        private int cutsPerMultiplierStep = 5;

        [Header("Meter Decay Settings")]
        [SerializeField] [Tooltip("Does the combo meter slowly drain if no cuts occur?")]
        private bool enableMeterDecay = false;

        [SerializeField] [Tooltip("Decay speed of combo meter per second")]
        private float meterDecaySpeed = 0.1f;

        private int consecutiveCuts = 0;
        private int currentMultiplier = 1;
        private float meterProgress = 0f; // 0.0 to 1.0 within the current multiplier or overall

        public int CurrentMultiplier => currentMultiplier;
        public int ConsecutiveCuts => consecutiveCuts;
        public float MeterProgress => meterProgress;

        public event Action<int, float> OnComboUpdated; // (multiplier, meterFill01)
        public event Action<int> OnMultiplierChanged;
        public event Action OnComboReset;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (enableMeterDecay && meterProgress > 0f && GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying())
            {
                meterProgress = Mathf.Max(0f, meterProgress - meterDecaySpeed * Time.deltaTime);
                OnComboUpdated?.Invoke(currentMultiplier, meterProgress);
            }
        }

        public void RegisterValidCut()
        {
            consecutiveCuts++;

            // Calculate progress towards next multiplier
            int stepProgress = consecutiveCuts % cutsPerMultiplierStep;
            meterProgress = (float)stepProgress / cutsPerMultiplierStep;

            int newMultiplier = Mathf.Min(maxMultiplier, 1 + (consecutiveCuts / cutsPerMultiplierStep));
            if (newMultiplier != currentMultiplier)
            {
                currentMultiplier = newMultiplier;
                meterProgress = 1f;
                OnMultiplierChanged?.Invoke(currentMultiplier);
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayComboIncrease(currentMultiplier);
                }
                if (CameraFeedback.Instance != null)
                {
                    CameraFeedback.Instance.PlayComboShake();
                }
                if (HapticManager.Instance != null)
                {
                    HapticManager.Instance.TriggerComboMilestone();
                }
            }

            OnComboUpdated?.Invoke(currentMultiplier, meterProgress);
        }

        public void ResetCombo()
        {
            if (consecutiveCuts == 0 && currentMultiplier == 1 && meterProgress == 0f) return;

            consecutiveCuts = 0;
            currentMultiplier = 1;
            meterProgress = 0f;

            OnComboReset?.Invoke();
            OnComboUpdated?.Invoke(currentMultiplier, meterProgress);
        }

        public void ReduceCombo(int amount = 2)
        {
            consecutiveCuts = Mathf.Max(0, consecutiveCuts - amount);
            currentMultiplier = Mathf.Min(maxMultiplier, 1 + (consecutiveCuts / cutsPerMultiplierStep));
            int stepProgress = consecutiveCuts % cutsPerMultiplierStep;
            meterProgress = (float)stepProgress / cutsPerMultiplierStep;

            OnComboUpdated?.Invoke(currentMultiplier, meterProgress);
        }
    }
}
