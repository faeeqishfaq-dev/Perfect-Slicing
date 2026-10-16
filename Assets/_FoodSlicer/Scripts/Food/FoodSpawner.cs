using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodSlicer
{
    public class FoodSpawner : MonoBehaviour
    {
        public static FoodSpawner Instance { get; private set; }

        [Header("Spawn Transforms")]
        [SerializeField] [Tooltip("Transform where foods originate in the distance")]
        private Transform spawnPoint;

        [SerializeField] [Tooltip("Parent container for spawned foods in hierarchy")]
        private Transform foodContainer;

        [Header("Lane & Spacing Defaults")]
        [SerializeField] [Tooltip("Distance ahead where foods spawn")]
        private float defaultSpawnDistanceZ = 22f;

        [SerializeField] [Tooltip("Lateral lane offset range (X axis)")]
        private float laneWidth = 0.15f;

        [SerializeField] [Tooltip("Base spacing between foods")]
        private float defaultSpacing = 3.2f;

        [Header("Timed Speed Progression")]
        [SerializeField] [Tooltip("Interval in seconds between speed increases")]
        private float speedIncreaseInterval = 3f;

        [SerializeField] [Tooltip("Percentage increase for the first interval (e.g. 10 for +10%)")]
        private float firstIncreasePercent = 10f;

        [SerializeField] [Tooltip("Percentage increase step for subsequent intervals (e.g. 5 for +5%)")]
        private float increaseStepPercent = 5f;

        [SerializeField] [Tooltip("Maximum speed increase percentage over base speed (e.g. 50 for +50%)")]
        private float maxIncreasePercent = 50f;

        [SerializeField] [Tooltip("Duration in seconds for smooth speed transition")]
        private float speedTransitionDuration = 0.25f;

        [SerializeField] [Tooltip("Multiplier applied to target required amounts to lengthen the level")]
        private int targetRequirementMultiplier = 2;

        private LevelData currentLevelData;
        private Coroutine spawnCoroutine;
        private float originalConveyorSpeed = 3.5f;
        private float currentConveyorSpeed = 4f;
        private float currentSpeedMultiplier = 1f;
        private float elapsedProgressionTime = 0f;
        private Coroutine speedProgressionCoroutine;
        private readonly List<FoodItem> activeSpawnedFoods = new List<FoodItem>();
        private readonly List<FoodMover> activeSpawnedMovers = new List<FoodMover>();

        // Balanced Spawning State
        private readonly List<FoodType> spawnQueue = new List<FoodType>();
        private int currentSpawnIndex = 0;
        private FoodType lastSpawnedType = (FoodType)(-1);
        private int consecutiveSpawnCount = 0;
        private readonly Dictionary<FoodType, FoodData> foodDataCache = new Dictionary<FoodType, FoodData>();

        public float OriginalConveyorSpeed => originalConveyorSpeed;
        public float CurrentConveyorSpeed => currentConveyorSpeed;
        public float CurrentSpeedMultiplier => currentSpeedMultiplier;
        public float ElapsedProgressionTime => elapsedProgressionTime;
        public int ActiveFoodCount => activeSpawnedFoods.Count;
        public int CurrentSpawnIndex => currentSpawnIndex;

        public float SpeedIncreaseInterval { get => speedIncreaseInterval; set => speedIncreaseInterval = value; }
        public float FirstIncreasePercent { get => firstIncreasePercent; set => firstIncreasePercent = value; }
        public float IncreaseStepPercent { get => increaseStepPercent; set => increaseStepPercent = value; }
        public float MaxIncreasePercent { get => maxIncreasePercent; set => maxIncreasePercent = value; }
        public float SpeedTransitionDuration { get => speedTransitionDuration; set => speedTransitionDuration = value; }
        public int TargetRequirementMultiplier { get => targetRequirementMultiplier; set => targetRequirementMultiplier = value; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (spawnPoint == null) spawnPoint = transform;
            if (foodContainer == null) foodContainer = transform;
        }

        private void OnDisable()
        {
            StopSpeedProgression();
        }

        private void OnDestroy()
        {
            StopSpeedProgression();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void StartSpawningForLevel(LevelData level)
        {
            currentLevelData = level;
            originalConveyorSpeed = level != null ? level.conveyorSpeed : 3.5f;
            currentConveyorSpeed = originalConveyorSpeed;

            ClearAllFoods();
            BuildBalancedSpawnQueue(level);

            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(SpawnSequenceRoutine());

            // Single timed speed progression coroutine
            StopSpeedProgression();
            speedProgressionCoroutine = StartCoroutine(TimedSpeedProgressionRoutine());
        }

        public void StopSpawning()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
            StopSpeedProgression();
        }

        public void StopSpeedProgression()
        {
            if (speedProgressionCoroutine != null)
            {
                StopCoroutine(speedProgressionCoroutine);
                speedProgressionCoroutine = null;
            }
            elapsedProgressionTime = 0f;
            currentSpeedMultiplier = 1f;

            if (originalConveyorSpeed > 0f)
            {
                currentConveyorSpeed = originalConveyorSpeed;
            }

            KnifeController knife = GetActiveKnife();
            if (knife != null)
            {
                knife.ResetSpeed();
            }
        }

        public void ClearAllFoods()
        {
            StopSpawning();
            for (int i = activeSpawnedFoods.Count - 1; i >= 0; i--)
            {
                if (activeSpawnedFoods[i] != null)
                {
                    if (PoolManager.Instance != null)
                    {
                        PoolManager.Instance.Despawn(activeSpawnedFoods[i].gameObject);
                    }
                    else
                    {
                        Destroy(activeSpawnedFoods[i].gameObject);
                    }
                }
            }
            activeSpawnedFoods.Clear();
            activeSpawnedMovers.Clear();
        }

        public IEnumerator CleanActiveFoodsRoutine(float duration)
        {
            StopSpawning();

            float elapsed = 0f;
            List<Transform> transformsToShrink = new List<Transform>();
            List<Vector3> startScales = new List<Vector3>();

            for (int i = 0; i < activeSpawnedFoods.Count; i++)
            {
                if (activeSpawnedFoods[i] != null)
                {
                    transformsToShrink.Add(activeSpawnedFoods[i].transform);
                    startScales.Add(activeSpawnedFoods[i].transform.localScale);
                }
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                for (int i = 0; i < transformsToShrink.Count; i++)
                {
                    if (transformsToShrink[i] != null)
                    {
                        transformsToShrink[i].localScale = Vector3.Lerp(startScales[i], Vector3.zero, t);
                    }
                }
                yield return null;
            }

            ClearAllFoods();
        }

        private void BuildBalancedSpawnQueue(LevelData level)
        {
            spawnQueue.Clear();
            currentSpawnIndex = 0;
            lastSpawnedType = (FoodType)(-1);
            consecutiveSpawnCount = 0;

            if (level == null || level.targetObjectives == null || level.targetObjectives.Count == 0)
            {
                return;
            }

            int mult = TargetManager.Instance != null ? TargetManager.Instance.TargetRequirementMultiplier : targetRequirementMultiplier;
            if (mult <= 0) mult = 2;

            // 1. Gather all required targets into a generation pool (scaled by targetRequirementMultiplier)
            List<FoodType> pool = new List<FoodType>();
            foreach (var obj in level.targetObjectives)
            {
                int totalCount = obj.requiredCount * mult;
                for (int i = 0; i < totalCount; i++)
                {
                    pool.Add(obj.foodType);
                }
            }

            // 2. Balanced interleaved shuffle with strict anti-repetition (never > 2 of same type)
            FoodType prev = (FoodType)(-1);
            int repeatCount = 0;

            while (pool.Count > 0)
            {
                List<FoodType> uniqueTypes = new List<FoodType>();
                for (int i = 0; i < pool.Count; i++)
                {
                    if (!uniqueTypes.Contains(pool[i])) uniqueTypes.Add(pool[i]);
                }

                List<FoodType> candidates = new List<FoodType>(uniqueTypes);
                if (uniqueTypes.Count > 1 && repeatCount >= 2)
                {
                    candidates.Remove(prev);
                }

                FoodType chosen = candidates[Random.Range(0, candidates.Count)];
                pool.Remove(chosen);
                spawnQueue.Add(chosen);

                if (chosen == prev)
                {
                    repeatCount++;
                }
                else
                {
                    prev = chosen;
                    repeatCount = 1;
                }
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append($"[FoodSpawner] Balanced Spawn Queue Built ({spawnQueue.Count} foods): ");
            for (int i = 0; i < spawnQueue.Count; i++)
            {
                sb.Append(spawnQueue[i]);
                if (i < spawnQueue.Count - 1) sb.Append(" -> ");
            }
            Debug.Log(sb.ToString());
        }

        private IEnumerator SpawnSequenceRoutine()
        {
            if (spawnQueue.Count == 0)
            {
                yield break;
            }

            float spacing = currentLevelData != null && currentLevelData.spawnSpacing > 0 ? currentLevelData.spawnSpacing : defaultSpacing;
            float spawnZ = currentLevelData != null && currentLevelData.spawnDistanceZ > 0 ? currentLevelData.spawnDistanceZ : defaultSpawnDistanceZ;

            // 1. Pre-spawn initial foods along the conveyor so the track is immediately active
            int preSpawnCount = Mathf.Min(3, spawnQueue.Count);
            float startKnifeZ = 0.50f;
            for (int i = 0; i < preSpawnCount; i++)
            {
                FoodType type = spawnQueue[currentSpawnIndex];
                FoodData data = FindFoodDataForType(type);
                if (data != null && data.uncutPrefab != null)
                {
                    float initialZ = startKnifeZ + (i * spacing);
                    SpawnFoodItem(data, initialZ);
                    TrackSpawn(type);
                    currentSpawnIndex++;
                }
            }

            // 2. Sequential & Fallback spawning loop
            while (true)
            {
                // Check if targets are all complete
                if (TargetManager.Instance != null && TargetManager.Instance.AreAllTargetsComplete())
                {
                    break;
                }
                if (LevelManager.Instance != null && LevelManager.Instance.AreAllTargetsComplete())
                {
                    break;
                }

                float waitTime = spacing / Mathf.Max(1f, currentConveyorSpeed);
                yield return new WaitForSeconds(waitTime);

                // Pause check
                while (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying())
                {
                    yield return null;
                }

                // Check again after wait
                if (TargetManager.Instance != null && TargetManager.Instance.AreAllTargetsComplete())
                {
                    break;
                }
                if (LevelManager.Instance != null && LevelManager.Instance.AreAllTargetsComplete())
                {
                    break;
                }

                FoodType nextType;

                if (currentSpawnIndex < spawnQueue.Count)
                {
                    nextType = spawnQueue[currentSpawnIndex];

                    // If this type is already 100% complete, prioritize incomplete targets
                    if (TargetManager.Instance != null && TargetManager.Instance.IsTargetComplete(nextType))
                    {
                        List<FoodType> incomplete = TargetManager.Instance.GetIncompleteTargetTypes();
                        if (incomplete.Count > 0)
                        {
                            nextType = PickBalancedFallbackType(incomplete);
                        }
                    }
                }
                else
                {
                    // Fallback spawning when initial queue ends (e.g. player missed a fruit)
                    List<FoodType> incomplete = TargetManager.Instance != null
                        ? TargetManager.Instance.GetIncompleteTargetTypes()
                        : new List<FoodType>();

                    if (incomplete.Count == 0)
                    {
                        break; // All objectives complete!
                    }

                    nextType = PickBalancedFallbackType(incomplete);
                }

                FoodData data = FindFoodDataForType(nextType);
                if (data != null && data.uncutPrefab != null)
                {
                    SpawnFoodItem(data, spawnZ);
                    TrackSpawn(nextType);
                    currentSpawnIndex++;
                }
                else
                {
                    Debug.LogError($"[FoodSpawner] Could not spawn {nextType} because FoodData or uncutPrefab is missing!");
                    yield return new WaitForSeconds(0.5f);
                }
            }

            spawnCoroutine = null;
        }

        private FoodType PickBalancedFallbackType(List<FoodType> incomplete)
        {
            if (incomplete == null || incomplete.Count == 0) return FoodType.Apple;
            if (incomplete.Count == 1) return incomplete[0];

            List<FoodType> candidates = new List<FoodType>(incomplete);
            if (consecutiveSpawnCount >= 2 && candidates.Count > 1)
            {
                candidates.Remove(lastSpawnedType);
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        private void TrackSpawn(FoodType type)
        {
            if (type == lastSpawnedType)
            {
                consecutiveSpawnCount++;
            }
            else
            {
                lastSpawnedType = type;
                consecutiveSpawnCount = 1;
            }
            Debug.Log($"[FoodSpawner] SPAWN: {type} (consecutive: {consecutiveSpawnCount})");
        }

        public FoodData FindFoodDataForType(FoodType type)
        {
            if (foodDataCache.TryGetValue(type, out FoodData cached) && cached != null && cached.uncutPrefab != null)
            {
                return cached;
            }

            if (currentLevelData != null && currentLevelData.foodSequence != null)
            {
                var found = currentLevelData.foodSequence.Find(f => f != null && f.foodType == type);
                if (found != null && found.uncutPrefab != null)
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
                    if (f != null && f.foodType == type && f.uncutPrefab != null)
                    {
                        foodDataCache[type] = f;
                        return f;
                    }
                }
            }

            Debug.LogError($"[FoodSpawner] No prefab assigned for FoodType: {type}");
            return null;
        }

        private void SpawnFoodItem(FoodData data, float spawnZ)
        {
            float posX = Random.Range(-laneWidth, laneWidth);
            Vector3 pos = new Vector3(posX, 0.05f, spawnZ);
            Quaternion rot = Quaternion.identity;

            GameObject obj = PoolManager.Instance != null
                ? PoolManager.Instance.Spawn(data.uncutPrefab, pos, rot, foodContainer)
                : Instantiate(data.uncutPrefab, pos, rot, foodContainer);

            FoodItem foodItem = obj.GetComponent<FoodItem>();
            if (foodItem == null) foodItem = obj.AddComponent<FoodItem>();

            bool isTarget = TargetManager.Instance != null ? TargetManager.Instance.IsTargetType(data.foodType) : true;
            foodItem.Initialize(data, isTarget);

            FoodMover mover = obj.GetComponent<FoodMover>();
            if (mover == null) mover = obj.AddComponent<FoodMover>();
            mover.CurrentSpeed = currentConveyorSpeed;
            mover.ResumeMoving();

            activeSpawnedFoods.Add(foodItem);
            activeSpawnedMovers.Add(mover);
        }

        public void AccelerateConveyor(float amount, float maxSpeed)
        {
            currentConveyorSpeed = Mathf.Min(maxSpeed, currentConveyorSpeed + amount);
            for (int i = 0; i < activeSpawnedMovers.Count; i++)
            {
                if (activeSpawnedMovers[i] != null)
                {
                    activeSpawnedMovers[i].CurrentSpeed = currentConveyorSpeed;
                }
            }
        }

        private IEnumerator TimedSpeedProgressionRoutine()
        {
            elapsedProgressionTime = 0f;
            currentSpeedMultiplier = 1f;
            float startMultiplier = 1f;
            float targetMultiplier = 1f;
            int lastIntervalIndex = 0;
            float transitionElapsed = speedTransitionDuration;

            ApplySpeedMultiplier(1f);

            while (true)
            {
                yield return null;

                if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying())
                {
                    continue;
                }

                elapsedProgressionTime += Time.deltaTime;

                int currentInterval = Mathf.FloorToInt(elapsedProgressionTime / speedIncreaseInterval);
                if (currentInterval > lastIntervalIndex)
                {
                    lastIntervalIndex = currentInterval;
                    float addedPercent = firstIncreasePercent + (currentInterval - 1) * increaseStepPercent;
                    float clampedPercent = Mathf.Min(addedPercent, maxIncreasePercent);

                    startMultiplier = currentSpeedMultiplier;
                    targetMultiplier = 1f + (clampedPercent / 100f);
                    transitionElapsed = 0f;

                    Debug.Log($"[FoodSpawner] Timed Speed Progression Step {currentInterval}: Target multiplier {targetMultiplier:F2} (+{clampedPercent:F0}%) at {elapsedProgressionTime:F2}s");
                }

                if (transitionElapsed < speedTransitionDuration)
                {
                    transitionElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(transitionElapsed / speedTransitionDuration);
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);
                    currentSpeedMultiplier = Mathf.Lerp(startMultiplier, targetMultiplier, smoothT);
                    ApplySpeedMultiplier(currentSpeedMultiplier);
                }
                else if (currentSpeedMultiplier != targetMultiplier)
                {
                    currentSpeedMultiplier = targetMultiplier;
                    ApplySpeedMultiplier(currentSpeedMultiplier);
                }
            }
        }

        private void ApplySpeedMultiplier(float multiplier)
        {
            currentConveyorSpeed = originalConveyorSpeed * multiplier;

            for (int i = activeSpawnedFoods.Count - 1; i >= 0; i--)
            {
                if (activeSpawnedFoods[i] != null && i < activeSpawnedMovers.Count && activeSpawnedMovers[i] != null)
                {
                    activeSpawnedMovers[i].CurrentSpeed = currentConveyorSpeed;
                }
                else if (activeSpawnedFoods[i] == null)
                {
                    activeSpawnedFoods.RemoveAt(i);
                    if (i < activeSpawnedMovers.Count) activeSpawnedMovers.RemoveAt(i);
                }
            }

            KnifeController knife = GetActiveKnife();
            if (knife != null)
            {
                knife.SetSpeedMultiplier(multiplier);
            }
        }

        private KnifeController cachedKnife;

        private KnifeController GetActiveKnife()
        {
            if (GameManager.Instance != null && GameManager.Instance.ActiveKnife != null)
            {
                return GameManager.Instance.ActiveKnife;
            }
            if (KnifeInputController.Instance != null && KnifeInputController.Instance.ActiveKnife != null)
            {
                return KnifeInputController.Instance.ActiveKnife;
            }
            if (cachedKnife == null)
            {
                cachedKnife = FindObjectOfType<KnifeController>();
            }
            return cachedKnife;
        }
    }
}
