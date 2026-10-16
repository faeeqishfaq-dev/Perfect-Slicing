using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoodSlicer
{
    [Serializable]
    public class TargetObjective
    {
        public FoodType foodType;
        public int requiredAmount;
        public int currentAmount;
        public TargetCountMode countMode = TargetCountMode.CompletedFood;
        public TargetSlotUI ui;

        public bool IsComplete => currentAmount >= requiredAmount;
        public float NormalizedProgress => requiredAmount > 0 ? Mathf.Clamp01((float)currentAmount / requiredAmount) : 1f;
    }

    public class TargetManager : MonoBehaviour
    {
        public static TargetManager Instance { get; private set; }

        [Header("Target Requirement Scaling")]
        [SerializeField] [Tooltip("Multiplier applied to target required amounts to lengthen the level")]
        private int targetRequirementMultiplier = 2;

        private readonly List<TargetObjective> activeObjectives = new List<TargetObjective>();
        private readonly Dictionary<FoodType, FoodData> foodDataCache = new Dictionary<FoodType, FoodData>();

        public List<TargetObjective> ActiveObjectives => activeObjectives;
        public int TargetRequirementMultiplier { get => targetRequirementMultiplier; set => targetRequirementMultiplier = value; }

        public event Action<TargetObjective> OnTargetProgressUpdated;
        public event Action OnAllTargetsCompleted;

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

        public void InitializeTargets(LevelData level, List<TargetSlotUI> slots)
        {
            activeObjectives.Clear();

            if (level == null || level.targetObjectives == null || level.targetObjectives.Count == 0)
            {
                Debug.LogWarning("[TargetManager] No target objectives found for level!");
                if (slots != null)
                {
                    foreach (var s in slots) if (s != null) s.HideSlot();
                }
                return;
            }

            int mult = targetRequirementMultiplier > 0 ? targetRequirementMultiplier : 2;
            if (FoodSpawner.Instance != null && FoodSpawner.Instance.TargetRequirementMultiplier > 0)
            {
                mult = FoodSpawner.Instance.TargetRequirementMultiplier;
            }

            Color[] slotFillColors = new Color[]
            {
                new Color(1.0f, 0.18f, 0.18f), // Red for slot 1
                new Color(1.0f, 0.72f, 0.05f), // Yellow/Amber for slot 2
                new Color(0.0f, 0.88f, 0.35f)  // Green for slot 3
            };

            for (int i = 0; i < level.targetObjectives.Count; i++)
            {
                var req = level.targetObjectives[i];
                FoodData data = ResolveFoodData(req.foodType, level);

                TargetSlotUI slot = (slots != null && i < slots.Count) ? slots[i] : null;

                int scaledRequiredCount = req.requiredCount * mult;

                TargetObjective obj = new TargetObjective
                {
                    foodType = req.foodType,
                    requiredAmount = scaledRequiredCount,
                    currentAmount = 0,
                    countMode = req.countMode,
                    ui = slot
                };

                if (slot != null)
                {
                    Sprite icon = data != null ? data.icon : null;
                    slot.SetupSlot(req.foodType, icon, scaledRequiredCount);
                    slot.SetBarColor(slotFillColors[i % slotFillColors.Length]);
                    slot.UpdateProgress(0);
                }

                activeObjectives.Add(obj);
                Debug.Log($"[TargetManager] TARGET {i + 1}: {req.foodType} required {scaledRequiredCount} (base {req.requiredCount} x {mult})");
            }

            // Hide any unused slots
            if (slots != null)
            {
                for (int i = level.targetObjectives.Count; i < slots.Count; i++)
                {
                    if (slots[i] != null) slots[i].HideSlot();
                }
            }
        }

        public void ReportFoodCompleted(FoodType type)
        {
            TargetObjective target = activeObjectives.Find(o => o.foodType == type);
            if (target != null && !target.IsComplete && target.countMode == TargetCountMode.CompletedFood)
            {
                target.currentAmount = Mathf.Min(target.currentAmount + 1, target.requiredAmount);
                if (target.ui != null)
                {
                    target.ui.UpdateProgress(target.currentAmount);
                }

                Debug.Log($"[TargetManager] TARGET PROGRESS: {type} {target.currentAmount}/{target.requiredAmount}");
                OnTargetProgressUpdated?.Invoke(target);

                if (AreAllTargetsComplete())
                {
                    Debug.Log("<color=green>[TargetManager] ALL TARGET OBJECTIVES COMPLETE!</color>");
                    OnAllTargetsCompleted?.Invoke();
                }
            }
        }

        public void ReportFoodSegmentCut(FoodType type)
        {
            TargetObjective target = activeObjectives.Find(o => o.foodType == type);
            if (target != null && !target.IsComplete && target.countMode == TargetCountMode.IndividualSlice)
            {
                target.currentAmount = Mathf.Min(target.currentAmount + 1, target.requiredAmount);
                if (target.ui != null)
                {
                    target.ui.UpdateProgress(target.currentAmount);
                }

                Debug.Log($"[TargetManager] TARGET SLICE: {type} {target.currentAmount}/{target.requiredAmount}");
                OnTargetProgressUpdated?.Invoke(target);

                if (AreAllTargetsComplete())
                {
                    Debug.Log("<color=green>[TargetManager] ALL TARGET OBJECTIVES COMPLETE!</color>");
                    OnAllTargetsCompleted?.Invoke();
                }
            }
        }

        public bool AreAllTargetsComplete()
        {
            if (activeObjectives == null || activeObjectives.Count == 0) return false;
            foreach (var obj in activeObjectives)
            {
                if (!obj.IsComplete) return false;
            }
            return true;
        }

        public List<FoodType> GetIncompleteTargetTypes()
        {
            List<FoodType> list = new List<FoodType>();
            foreach (var obj in activeObjectives)
            {
                if (!obj.IsComplete)
                {
                    list.Add(obj.foodType);
                }
            }
            return list;
        }

        public bool IsTargetType(FoodType type)
        {
            foreach (var obj in activeObjectives)
            {
                if (obj.foodType == type) return true;
            }
            return false;
        }

        public bool IsTargetComplete(FoodType type)
        {
            TargetObjective target = activeObjectives.Find(o => o.foodType == type);
            return target != null && target.IsComplete;
        }

        public int GetRemainingRequirement(FoodType type)
        {
            TargetObjective target = activeObjectives.Find(o => o.foodType == type);
            return target != null ? Mathf.Max(0, target.requiredAmount - target.currentAmount) : 0;
        }

        private FoodData ResolveFoodData(FoodType type, LevelData level)
        {
            if (foodDataCache.TryGetValue(type, out FoodData cached) && cached != null)
            {
                return cached;
            }

            if (level != null && level.foodSequence != null)
            {
                var found = level.foodSequence.Find(f => f != null && f.foodType == type);
                if (found != null)
                {
                    foodDataCache[type] = found;
                    return found;
                }
            }

            var all = Resources.LoadAll<FoodData>("Foods");
            if (all != null)
            {
                foreach (var f in all)
                {
                    if (f != null && f.foodType == type)
                    {
                        foodDataCache[type] = f;
                        return f;
                    }
                }
            }

            Debug.LogError($"[TargetManager] No FoodData found for FoodType: {type}");
            return null;
        }
    }
}
