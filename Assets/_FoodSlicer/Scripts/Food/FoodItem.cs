using System;
using UnityEngine;

namespace FoodSlicer
{
    [RequireComponent(typeof(Collider))]
    public class FoodItem : MonoBehaviour
    {
        [Header("Food References")]
        [SerializeField] private FoodType foodType;
        [SerializeField] private FoodData foodData;
        [SerializeField] private SegmentedFood segmentedFood;
        [SerializeField] private ParticleSystem customSliceVFX;

        private bool isTargetFood = false;
        private bool isCompleted = false;
        private bool objectiveProgressReported = false;
        private Collider col;

        public FoodType Type => foodData != null ? foodData.foodType : foodType;
        public FoodData Data => foodData;
        public bool IsTarget => isTargetFood;
        public bool IsCompleted => isCompleted;
        public SegmentedFood Segments => segmentedFood;

        private void Awake()
        {
            col = GetComponent<Collider>();
            if (segmentedFood == null) segmentedFood = GetComponent<SegmentedFood>();
        }

        public void Initialize(FoodData data, bool isTarget)
        {
            foodData = data;
            if (data != null) foodType = data.foodType;
            isTargetFood = isTarget;
            isCompleted = false;
            objectiveProgressReported = false;

            if (col != null) col.enabled = true;

            if (data != null)
            {
                if (data.visualScale != Vector3.zero)
                {
                    transform.localScale = data.visualScale;
                }
                if (data.boardHeightOffset != 0f)
                {
                    Vector3 pos = transform.position;
                    pos.y += data.boardHeightOffset;
                    transform.position = pos;
                }
            }

            if (segmentedFood != null)
            {
                segmentedFood.Setup(data, isTarget);
            }
        }

        public void SetTarget(bool isTarget)
        {
            isTargetFood = isTarget;
        }

        public bool Slice(Vector3 cutPosition, out bool isPerfect)
        {
            isPerfect = false;
            if (isCompleted) return false;
            if (segmentedFood == null) segmentedFood = GetComponent<SegmentedFood>();

            if (segmentedFood != null)
            {
                bool cutSuccess = segmentedFood.CutNextSegment(cutPosition, out isPerfect);
                if (cutSuccess)
                {
                    OnSegmentSliced(cutPosition, isPerfect);
                    return true;
                }
            }
            return false;
        }

        private void OnSegmentSliced(Vector3 cutPosition, bool isPerfect)
        {
            // Spawn juice / crumb particles
            SpawnSliceParticles(cutPosition);

            // Audio & Haptic
            FoodType type = foodData != null ? foodData.foodType : FoodType.Apple;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySlice(type);
                if (isPerfect)
                {
                    AudioManager.Instance.PlayPerfectSlice();
                }
            }

            if (HapticManager.Instance != null)
            {
                if (isPerfect) HapticManager.Instance.TriggerPerfectSlice();
                else HapticManager.Instance.TriggerSlice();
            }

            // Camera feedback
            if (CameraFeedback.Instance != null)
            {
                if (isPerfect) CameraFeedback.Instance.PlayPerfectShake();
                else CameraFeedback.Instance.PlaySliceShake();
            }

            // Hit stop
            if (HitStopManager.Instance != null)
            {
                HitStopManager.Instance.RequestHitStop(isPerfect ? 0.05f : 0.03f);
            }

            // Level & Score callbacks
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RegisterFoodSegmentCut(this, isPerfect, cutPosition);
            }
        }

        private void SpawnSliceParticles(Vector3 cutPosition)
        {
            if (VFXManager.Instance != null && foodData != null)
            {
                VFXManager.Instance.PlaySliceEffect(cutPosition, foodData);
            }
        }

        public void OnFullySliced()
        {
            if (isCompleted) return;
            isCompleted = true;
            if (col != null) col.enabled = false;

            if (!objectiveProgressReported)
            {
                objectiveProgressReported = true;
                if (TargetManager.Instance != null && isTargetFood)
                {
                    TargetManager.Instance.ReportFoodCompleted(Type);
                }

                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.RegisterFoodFullyCut(this);
                }
            }

            // Allow any remaining animations or small delay, then despawn
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(gameObject, 0.1f);
            }
            else
            {
                Destroy(gameObject, 0.1f);
            }
        }
    }
}
