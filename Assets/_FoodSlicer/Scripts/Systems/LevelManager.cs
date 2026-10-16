using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoodSlicer
{
    [Serializable]
    public class TargetProgress
    {
        public FoodType foodType;
        public int currentCount;
        public int requiredCount;
        public TargetCountMode countMode = TargetCountMode.CompletedFood;
        public bool IsComplete => currentCount >= requiredCount;
    }

    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Levels (20 Levels)")]
        [SerializeField] [Tooltip("All level configuration scriptable objects (levels 1 to 20)")]
        private List<LevelData> levels = new List<LevelData>();

        [Header("Current Runtime Level")]
        [SerializeField] private int activeLevelIndex = 0; // 0-based (Level 1 is 0)

        [Header("Level Complete Presentation")]
        [SerializeField] private GameObject knifeRestPillow;
        [SerializeField] private Transform pillowRestPoint;

        private LevelData currentLevelData;
        private readonly List<TargetProgress> activeTargets = new List<TargetProgress>();
        private bool isLevelWon = false;
        private bool isLevelFailed = false;
        private bool levelCompletionTriggered = false;
        private Coroutine levelWinCoroutine;

        public LevelData CurrentLevelData => currentLevelData;
        public bool IsLevelWon => isLevelWon;
        public bool IsLevelFailed => isLevelFailed;
        public List<TargetProgress> ActiveTargets => activeTargets;
        public int ActiveLevelNumber => activeLevelIndex + 1;
        public int TotalLevelCount => levels != null ? levels.Count : 0;
        public GameObject KnifeRestPillow { get => knifeRestPillow; set => knifeRestPillow = value; }
        public Transform PillowRestPoint { get => pillowRestPoint; set => pillowRestPoint = value; }

        public event Action<LevelData> OnLevelStarted;
        public event Action<FoodType, int, int> OnTargetUpdated; // (type, current, required)
        public event Action<int, int> OnLevelWon; // (earnedCoins, totalScore)
        public event Action<string> OnLevelFailed; // (reason)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (levels == null || levels.Count == 0)
            {
                var loaded = Resources.LoadAll<LevelData>("Levels");
                if (loaded != null && loaded.Length > 0)
                {
                    levels = new List<LevelData>(loaded);
                    levels.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            int savedLevel = SaveManager.Instance != null ? SaveManager.Instance.GetUnlockedLevel() : 1;
            activeLevelIndex = Mathf.Clamp(savedLevel - 1, 0, levels.Count - 1);

            StartCurrentLevel();
        }

        public void StartCurrentLevel()
        {
            if (levels == null || levels.Count == 0)
            {
                Debug.LogWarning("[LevelManager] No levels assigned!");
                return;
            }

            activeLevelIndex = Mathf.Clamp(activeLevelIndex, 0, levels.Count - 1);
            currentLevelData = levels[activeLevelIndex];

            levelCompletionTriggered = false;
            isLevelWon = false;
            isLevelFailed = false;

            Time.timeScale = 1f;

            if (levelWinCoroutine != null)
            {
                StopCoroutine(levelWinCoroutine);
                levelWinCoroutine = null;
            }

            if (LevelCompletePresentation.Instance != null)
            {
                LevelCompletePresentation.Instance.ResetPresentation();
            }
            else if (knifeRestPillow != null)
            {
                knifeRestPillow.SetActive(false);
            }

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ResetState(GameState.Playing);
            }

            if (GameManager.Instance != null && GameManager.Instance.ActiveKnife != null)
            {
                GameManager.Instance.ActiveKnife.ResetKnifeToIdle();
                GameManager.Instance.ActiveKnife.ResetSpeed();
                if (KnifeInputController.Instance != null)
                {
                    KnifeInputController.Instance.SetActiveKnife(GameManager.Instance.ActiveKnife);
                    KnifeInputController.Instance.SetInputActive(true);
                }
            }
            else if (KnifeInputController.Instance != null)
            {
                KnifeInputController.Instance.SetInputActive(true);
            }

            // Clean active foods on conveyor
            if (FoodSpawner.Instance != null)
            {
                FoodSpawner.Instance.ClearAllFoods();
            }

            // Reset Score & Combo
            if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
            if (ComboManager.Instance != null) ComboManager.Instance.ResetCombo();

            // Setup Target Objectives via authoritative TargetManager
            if (TargetManager.Instance != null && UIManager.Instance != null)
            {
                TargetManager.Instance.InitializeTargets(currentLevelData, UIManager.Instance.TargetSlots);
            }

            activeTargets.Clear();
            if (currentLevelData.targetObjectives != null)
            {
                int mult = TargetManager.Instance != null ? TargetManager.Instance.TargetRequirementMultiplier : 2;
                foreach (var obj in currentLevelData.targetObjectives)
                {
                    activeTargets.Add(new TargetProgress
                    {
                        foodType = obj.foodType,
                        currentCount = 0,
                        requiredCount = obj.requiredCount * mult,
                        countMode = obj.countMode
                    });
                }
            }

            OnLevelStarted?.Invoke(currentLevelData);

            // Notify initial target state
            foreach (var t in activeTargets)
            {
                OnTargetUpdated?.Invoke(t.foodType, t.currentCount, t.requiredCount);
            }

            // Start spawner
            if (Application.isPlaying && FoodSpawner.Instance != null)
            {
                FoodSpawner.Instance.StartSpawningForLevel(currentLevelData);
            }
        }

        public void RegisterFoodSegmentCut(FoodItem food, bool isPerfect, Vector3 cutPos)
        {
            if (isLevelWon || isLevelFailed || levelCompletionTriggered) return;

            int comboMult = ComboManager.Instance != null ? ComboManager.Instance.CurrentMultiplier : 1;
            int baseScore = food.Data != null ? food.Data.scorePerSegment : 10;

            if (food.IsTarget)
            {
                int pts = ScoreManager.Instance != null ? ScoreManager.Instance.AddSliceScore(baseScore, isPerfect, comboMult) : baseScore;
                if (FloatingTextManager.Instance != null)
                {
                    FloatingTextManager.Instance.ShowScorePopup(cutPos, pts, isPerfect);
                }
                if (ComboManager.Instance != null)
                {
                    ComboManager.Instance.RegisterValidCut();
                }

                // Conveyor slight acceleration
                if (FoodSpawner.Instance != null && currentLevelData != null)
                {
                    FoodSpawner.Instance.AccelerateConveyor(currentLevelData.speedIncreasePerCut, currentLevelData.maxConveyorSpeed);
                }

                FoodType type = food.Type;
                if (TargetManager.Instance != null)
                {
                    TargetManager.Instance.ReportFoodSegmentCut(type);
                }
                else
                {
                    TargetProgress target = activeTargets.Find(t => t.foodType == type);
                    if (target != null && !target.IsComplete && target.countMode == TargetCountMode.IndividualSlice)
                    {
                        target.currentCount = Mathf.Min(target.currentCount + 1, target.requiredCount);
                        OnTargetUpdated?.Invoke(target.foodType, target.currentCount, target.requiredCount);
                    }
                }
                CheckWinCondition();
            }
            else
            {
                // Non-target food cut handling
                HandleWrongFoodCut();
            }
        }

        public void RegisterFoodFullyCut(FoodItem food)
        {
            if (isLevelWon || isLevelFailed || levelCompletionTriggered) return;

            FoodType type = food != null ? food.Type : FoodType.Apple;
            if (TargetManager.Instance != null)
            {
                TargetManager.Instance.ReportFoodCompleted(type);
            }
            else
            {
                TargetProgress target = activeTargets.Find(t => t.foodType == type);
                if (target != null && !target.IsComplete && target.countMode == TargetCountMode.CompletedFood)
                {
                    target.currentCount = Mathf.Min(target.currentCount + 1, target.requiredCount);
                    OnTargetUpdated?.Invoke(target.foodType, target.currentCount, target.requiredCount);
                }
            }
            CheckWinCondition();
        }

        public bool AreAllTargetsComplete()
        {
            if (TargetManager.Instance != null)
            {
                return TargetManager.Instance.AreAllTargetsComplete();
            }
            if (activeTargets == null || activeTargets.Count == 0) return false;
            foreach (var t in activeTargets)
            {
                if (!t.IsComplete) return false;
            }
            return true;
        }

        private void CheckWinCondition()
        {
            if (levelCompletionTriggered || isLevelWon || isLevelFailed) return;

            if (AreAllTargetsComplete())
            {
                levelCompletionTriggered = true;
                if (levelWinCoroutine != null) StopCoroutine(levelWinCoroutine);
                levelWinCoroutine = StartCoroutine(LevelWinSequenceRoutine());
            }
        }

        private IEnumerator LevelWinSequenceRoutine()
        {
            // 1. Wait for target UI progress bars to visibly reach 100%
            float uiFillWait = 0.22f;
            if (UIManager.Instance != null)
            {
                uiFillWait = UIManager.Instance.TargetProgressAnimationDuration;
            }
            yield return new WaitForSeconds(uiFillWait + 0.05f);

            isLevelWon = true;

            // 2. Lock gameplay input & pause spawner
            if (KnifeInputController.Instance != null)
            {
                KnifeInputController.Instance.SetInputActive(false);
            }
            if (FoodSpawner.Instance != null)
            {
                FoodSpawner.Instance.StopSpawning();
            }

            // 3. Clean remaining active conveyor foods smoothly
            if (FoodSpawner.Instance != null)
            {
                yield return StartCoroutine(FoodSpawner.Instance.CleanActiveFoodsRoutine(0.35f));
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
            }

            // 4. Prepare active knife
            KnifeController knife = GameManager.Instance != null ? GameManager.Instance.ActiveKnife : null;
            if (knife != null)
            {
                knife.PrepareForCompletion();
            }

            // 5. Play celebratory knife flips and presentation
            if (LevelCompletePresentation.Instance != null)
            {
                yield return StartCoroutine(LevelCompletePresentation.Instance.PlayCompletionRoutine(knife));
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            // 6. Set GameState and update economy/progression
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetState(GameState.LevelComplete);
            }

            int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
            int coinsEarned = currentLevelData != null ? currentLevelData.rewardCoins : 50;

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.UnlockLevel(activeLevelIndex + 2);
                SaveManager.Instance.AddCoins(coinsEarned);
            }

            // 7. Fire OnLevelWon to trigger LevelCompletePanel popup in UIManager
            OnLevelWon?.Invoke(coinsEarned, finalScore);
            levelWinCoroutine = null;
        }

        public void RegisterBoardHit()
        {
            if (isLevelWon || isLevelFailed) return;

            BoardHitMode mode = currentLevelData != null ? currentLevelData.boardHitMode : BoardHitMode.Penalty;

            switch (mode)
            {
                case BoardHitMode.FailLevel:
                    TriggerLevelFail("Hit the cutting board!");
                    break;
                case BoardHitMode.ResetCombo:
                    if (ComboManager.Instance != null) ComboManager.Instance.ResetCombo();
                    break;
                case BoardHitMode.Penalty:
                    int penalty = currentLevelData != null ? currentLevelData.boardPenaltyScore : 15;
                    if (ScoreManager.Instance != null) ScoreManager.Instance.DeductScore(penalty);
                    if (ComboManager.Instance != null) ComboManager.Instance.ResetCombo();
                    break;
            }
        }

        private void HandleWrongFoodCut()
        {
            WrongFoodPenaltyMode mode = currentLevelData != null ? currentLevelData.wrongFoodPenaltyMode : WrongFoodPenaltyMode.None;

            switch (mode)
            {
                case WrongFoodPenaltyMode.LoseScore:
                    if (ScoreManager.Instance != null) ScoreManager.Instance.DeductScore(10);
                    break;
                case WrongFoodPenaltyMode.ResetCombo:
                    if (ComboManager.Instance != null) ComboManager.Instance.ResetCombo();
                    break;
                case WrongFoodPenaltyMode.FailLevel:
                    TriggerLevelFail("Cut incorrect food!");
                    break;
                case WrongFoodPenaltyMode.None:
                default:
                    // Slicing still gives minimal points without progressing targets
                    if (ScoreManager.Instance != null) ScoreManager.Instance.AddSliceScore(5, false, 1);
                    break;
            }
        }

        public void RegisterFoodMissed(FoodItem food)
        {
            if (isLevelWon || isLevelFailed) return;

            if (food != null && food.IsTarget)
            {
                MissPenaltyMode mode = currentLevelData != null ? currentLevelData.missPenaltyMode : MissPenaltyMode.ResetCombo;
                if (mode == MissPenaltyMode.ResetCombo && ComboManager.Instance != null)
                {
                    ComboManager.Instance.ResetCombo();
                }
                else if (mode == MissPenaltyMode.LoseScore && ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.DeductScore(15);
                }
            }
        }

        public void FailLevel(string reason = "Level Failed")
        {
            TriggerLevelFail(reason);
        }

        public void TriggerLevelFail(string reason)
        {
            if (isLevelWon || isLevelFailed) return;
            isLevelFailed = true;

            // Stop any completion routine to prevent completion on fail
            if (levelWinCoroutine != null)
            {
                StopCoroutine(levelWinCoroutine);
                levelWinCoroutine = null;
            }

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetState(GameState.LevelFailed);
            }

            // Disable knife input
            if (KnifeInputController.Instance != null)
            {
                KnifeInputController.Instance.SetInputActive(false);
            }

            // Stop food spawning
            if (FoodSpawner.Instance != null)
            {
                FoodSpawner.Instance.StopSpawning();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLevelFail();
            }

            OnLevelFailed?.Invoke(reason);
        }

        public void NextLevel()
        {
            Time.timeScale = 1f;
            activeLevelIndex = (activeLevelIndex + 1) % levels.Count;
            StartCurrentLevel();
        }

        public void ReplayLevel()
        {
            Time.timeScale = 1f;
            StartCurrentLevel();
        }

        public void LoadLevelIndex(int index)
        {
            Time.timeScale = 1f;
            activeLevelIndex = Mathf.Clamp(index, 0, levels.Count - 1);
            StartCurrentLevel();
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetState(GameState.Menu);
            }
            SceneManager.LoadScene("MainMenu");
        }
    }
}
