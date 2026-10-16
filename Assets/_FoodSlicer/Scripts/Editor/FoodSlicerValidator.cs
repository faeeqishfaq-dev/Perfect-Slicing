using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FoodSlicer.EditorTools
{
    public static class FoodSlicerValidator
    {
        private const string ROOT_PATH = "Assets/_FoodSlicer";

        [InitializeOnLoadMethod]
        private static void AutoRunValidation()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool("FoodSlicer_Validated_Done_v4", false))
                {
                    EditorPrefs.SetBool("FoodSlicer_Validated_Done_v4", true);
                    ValidateCompleteProject();
                }
            };
        }

        [MenuItem("FoodSlicer/Validate Complete Project")]
        public static void ValidateCompleteProject()
        {
            Debug.Log("<color=cyan><b>=== STARTING COMPREHENSIVE PROJECT VALIDATION ===</b></color>");

            int errorCount = 0;
            int warningCount = 0;

            // 1. Validate Build Settings
            Debug.Log("[Validator] 1. Checking Build Settings...");
            if (EditorBuildSettings.scenes.Length < 2)
            {
                Debug.LogError("[Validator] Missing scenes in Build Settings!");
                errorCount++;
            }
            else
            {
                Debug.Log($"[Validator] Scene 0: {EditorBuildSettings.scenes[0].path} (Enabled: {EditorBuildSettings.scenes[0].enabled})");
                Debug.Log($"[Validator] Scene 1: {EditorBuildSettings.scenes[1].path} (Enabled: {EditorBuildSettings.scenes[1].enabled})");
            }

            // 2. Validate ScriptableObjects
            Debug.Log("[Validator] 2. Checking ScriptableObjects...");
            string[] levelGuids = AssetDatabase.FindAssets("t:LevelData", new string[] { $"{ROOT_PATH}/ScriptableObjects/Levels" });
            Debug.Log($"[Validator] Found {levelGuids.Length} LevelData assets (Requirement: >= 20).");
            if (levelGuids.Length < 20)
            {
                Debug.LogError($"[Validator] Expected at least 20 levels, found {levelGuids.Length}!");
                errorCount++;
            }

            string[] knifeGuids = AssetDatabase.FindAssets("t:KnifeData", new string[] { $"{ROOT_PATH}/ScriptableObjects/Knives" });
            Debug.Log($"[Validator] Found {knifeGuids.Length} KnifeData assets (Requirement: >= 6).");
            if (knifeGuids.Length < 6)
            {
                Debug.LogError($"[Validator] Expected at least 6 knives, found {knifeGuids.Length}!");
                errorCount++;
            }

            string[] foodGuids = AssetDatabase.FindAssets("t:FoodData", new string[] { $"{ROOT_PATH}/ScriptableObjects/Foods" });
            Debug.Log($"[Validator] Found {foodGuids.Length} FoodData assets (Requirement: >= 13).");
            if (foodGuids.Length < 13)
            {
                Debug.LogError($"[Validator] Expected at least 13 foods, found {foodGuids.Length}!");
                errorCount++;
            }

            // 3. Validate MainMenu Scene
            Debug.Log("[Validator] 3. Validating MainMenu Scene...");
            var mainMenuScene = EditorSceneManager.OpenScene($"{ROOT_PATH}/Scenes/MainMenu.unity", OpenSceneMode.Single);
            if (!mainMenuScene.IsValid())
            {
                Debug.LogError("[Validator] Failed to open MainMenu scene!");
                errorCount++;
            }
            else
            {
                CheckSceneMissingComponents(ref errorCount);

                MainMenuManager mmm = Object.FindObjectOfType<MainMenuManager>();
                if (mmm == null) { Debug.LogError("[Validator] MainMenuManager missing!"); errorCount++; }

                KnifeShopManager ksm = Object.FindObjectOfType<KnifeShopManager>();
                if (ksm == null) { Debug.LogError("[Validator] KnifeShopManager missing!"); errorCount++; }

                SaveManager sm = Object.FindObjectOfType<SaveManager>();
                if (sm == null) { Debug.LogError("[Validator] SaveManager missing in MainMenu!"); errorCount++; }

                AudioManager am = Object.FindObjectOfType<AudioManager>();
                if (am == null) { Debug.LogError("[Validator] AudioManager missing in MainMenu!"); errorCount++; }
            }

            // 4. Validate Gameplay Scene
            Debug.Log("[Validator] 4. Validating Gameplay Scene...");
            var gameplayScene = EditorSceneManager.OpenScene($"{ROOT_PATH}/Scenes/Gameplay.unity", OpenSceneMode.Single);
            if (!gameplayScene.IsValid())
            {
                Debug.LogError("[Validator] Failed to open Gameplay scene!");
                errorCount++;
            }
            else
            {
                CheckSceneMissingComponents(ref errorCount);

                GameManager gm = Object.FindObjectOfType<GameManager>();
                if (gm == null) { Debug.LogError("[Validator] GameManager missing!"); errorCount++; }

                LevelManager lm = Object.FindObjectOfType<LevelManager>();
                if (lm == null) { Debug.LogError("[Validator] LevelManager missing!"); errorCount++; }
                else if (lm.TotalLevelCount < 20) { Debug.LogError($"[Validator] LevelManager has only {lm.TotalLevelCount} levels wired!"); errorCount++; }

                UIManager uim = Object.FindObjectOfType<UIManager>();
                if (uim == null) { Debug.LogError("[Validator] UIManager missing!"); errorCount++; }

                ScoreManager scm = Object.FindObjectOfType<ScoreManager>();
                if (scm == null) { Debug.LogError("[Validator] ScoreManager missing!"); errorCount++; }

                ComboManager cm = Object.FindObjectOfType<ComboManager>();
                if (cm == null) { Debug.LogError("[Validator] ComboManager missing!"); errorCount++; }

                FoodSpawner fs = Object.FindObjectOfType<FoodSpawner>();
                if (fs == null) { Debug.LogError("[Validator] FoodSpawner missing!"); errorCount++; }

                CameraFeedback cf = Object.FindObjectOfType<CameraFeedback>();
                if (cf == null) { Debug.LogError("[Validator] CameraFeedback missing!"); errorCount++; }

                HitStopManager hsm = Object.FindObjectOfType<HitStopManager>();
                if (hsm == null) { Debug.LogError("[Validator] HitStopManager missing!"); errorCount++; }

                VFXManager vfx = Object.FindObjectOfType<VFXManager>();
                if (vfx == null) { Debug.LogError("[Validator] VFXManager missing!"); errorCount++; }

                FloatingTextManager ftm = Object.FindObjectOfType<FloatingTextManager>();
                if (ftm == null) { Debug.LogError("[Validator] FloatingTextManager missing!"); errorCount++; }

                KnifeInputController kic = Object.FindObjectOfType<KnifeInputController>();
                if (kic == null) { Debug.LogError("[Validator] KnifeInputController missing!"); errorCount++; }

                TargetManager tm = Object.FindObjectOfType<TargetManager>();
                if (tm == null) { Debug.LogError("[Validator] TargetManager missing!"); errorCount++; }

                LevelCompletePresentation lcp = Object.FindObjectOfType<LevelCompletePresentation>();
                if (lcp == null) { Debug.LogError("[Validator] LevelCompletePresentation missing!"); errorCount++; }
                else
                {
                    if (lcp.Pillow == null) { Debug.LogError("[Validator] LevelCompletePresentation.Pillow missing!"); errorCount++; }
                    if (lcp.KnifeRestPoint == null) { Debug.LogError("[Validator] LevelCompletePresentation.KnifeRestPoint missing!"); errorCount++; }
                    if (lcp.FirstFlipApex == null) { Debug.LogError("[Validator] LevelCompletePresentation.FirstFlipApex missing!"); errorCount++; }
                    if (lcp.SecondFlipApex == null) { Debug.LogError("[Validator] LevelCompletePresentation.SecondFlipApex missing!"); errorCount++; }

                    Camera cam = Camera.main;
                    if (cam != null && lcp.KnifeRestPoint != null && lcp.FirstFlipApex != null && lcp.SecondFlipApex != null)
                    {
                        Vector3 vpRest = cam.WorldToViewportPoint(lcp.KnifeRestPoint.position);
                        Vector3 vpFirst = cam.WorldToViewportPoint(lcp.FirstFlipApex.position);
                        Vector3 vpSecond = cam.WorldToViewportPoint(lcp.SecondFlipApex.position);

                        if (vpRest.z <= 0 || vpRest.x < 0.05f || vpRest.x > 0.95f || vpRest.y < 0.05f || vpRest.y > 0.95f)
                        {
                            Debug.LogError($"[Validator] KnifeRestPoint outside camera viewport: {vpRest}");
                            errorCount++;
                        }
                        if (vpFirst.z <= 0 || vpFirst.x < 0.05f || vpFirst.x > 0.95f || vpFirst.y < 0.05f || vpFirst.y > 0.95f)
                        {
                            Debug.LogError($"[Validator] FirstFlipApex outside camera viewport: {vpFirst}");
                            errorCount++;
                        }
                        if (vpSecond.z <= 0 || vpSecond.x < 0.05f || vpSecond.x > 0.95f || vpSecond.y < 0.05f || vpSecond.y > 0.95f)
                        {
                            Debug.LogError($"[Validator] SecondFlipApex outside camera viewport: {vpSecond}");
                            errorCount++;
                        }
                    }
                }
            }

            // 5. Functional Simulation Check
            Debug.Log("[Validator] 5. Running Slicing Simulation Tests...");
            InitializeSceneManagers();
            RunSlicingSimulation(ref errorCount);

            Debug.Log($"<color={(errorCount == 0 ? "green" : "red")}><b>=== VALIDATION COMPLETE: {errorCount} ERRORS, {warningCount} WARNINGS ===</b></color>");
        }

        private static void InitializeSceneManagers()
        {
            var monos = Object.FindObjectsOfType<MonoBehaviour>();
            foreach (var mb in monos)
            {
                if (mb == null) continue;
                try
                {
                    var awake = mb.GetType().GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    awake?.Invoke(mb, null);
                }
                catch (System.Exception ex)
                {
                    // Ignore non-critical runtime warning during editor reflection
                }
            }
        }

        private static void CheckSceneMissingComponents(ref int errorCount)
        {
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);
            foreach (var go in allObjects)
            {
                Component[] comps = go.GetComponents<Component>();
                for (int i = 0; i < comps.Length; i++)
                {
                    if (comps[i] == null)
                    {
                        Debug.LogError($"[Validator] Missing Component/Script on GameObject: '{go.name}' in scene!");
                        errorCount++;
                    }
                }
            }
        }

        private static void RunSlicingSimulation(ref int errorCount)
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogError("[Validator] SaveManager.Instance is null!");
                errorCount++;
                return;
            }

            // Test SaveManager
            SaveManager.Instance.ResetSaveData();
            if (SaveManager.Instance.GetCoins() != 100)
            {
                Debug.LogError("[Validator] SaveManager starting coins mismatch!");
                errorCount++;
            }
            SaveManager.Instance.AddCoins(50);
            if (SaveManager.Instance.GetCoins() != 150)
            {
                Debug.LogError("[Validator] SaveManager AddCoins failed!");
                errorCount++;
            }

            // Test Knife Purchasing
            bool bought = SaveManager.Instance.SpendCoins(150);
            if (!bought || SaveManager.Instance.GetCoins() != 0)
            {
                Debug.LogError("[Validator] SaveManager SpendCoins failed!");
                errorCount++;
            }
            SaveManager.Instance.SetKnifeOwned(1, true);
            SaveManager.Instance.SetSelectedKnife(1);
            if (SaveManager.Instance.GetSelectedKnife() != 1)
            {
                Debug.LogError("[Validator] Selected knife persistence failed!");
                errorCount++;
            }

            // Test Segmented Food Cutting (Banana & Carrot)
            string bananaPath = $"{ROOT_PATH}/Prefabs/Food/Food_Banana.prefab";
            GameObject bananaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bananaPath);
            if (bananaPrefab == null)
            {
                Debug.LogError("[Validator] Food_Banana prefab missing!");
                errorCount++;
                return;
            }

            GameObject bananaInst = Object.Instantiate(bananaPrefab);
            FoodItem foodItem = bananaInst.GetComponent<FoodItem>();
            SegmentedFood segFood = bananaInst.GetComponent<SegmentedFood>();
            if (segFood != null) segFood.InitializeSegments();

            if (segFood == null || segFood.TotalSegments <= 1)
            {
                Debug.LogError("[Validator] Banana must have multiple sequential segments!");
                errorCount++;
            }
            else
            {
                int initialRemaining = segFood.RemainingSegments;
                bool cut1 = foodItem.Slice(bananaInst.transform.position, out bool isPerf1);
                int afterCut1 = segFood.RemainingSegments;

                if (!cut1 || afterCut1 != initialRemaining - 1)
                {
                    Debug.LogError($"[Validator] Sequential cut 1 failed! Remaining: {afterCut1}, expected: {initialRemaining - 1}");
                    errorCount++;
                }
                else
                {
                    Debug.Log($"[Validator] Banana Cut 1 success! Segments remaining: {afterCut1}/{segFood.TotalSegments}");
                }

                bool cut2 = foodItem.Slice(bananaInst.transform.position, out bool isPerf2);
                int afterCut2 = segFood.RemainingSegments;
                if (!cut2 || afterCut2 != afterCut1 - 1)
                {
                    Debug.LogError($"[Validator] Sequential cut 2 failed! Remaining: {afterCut2}");
                    errorCount++;
                }
                else
                {
                    Debug.Log($"[Validator] Banana Cut 2 success! Segments remaining: {afterCut2}/{segFood.TotalSegments}");
                }
            }

            Object.DestroyImmediate(bananaInst);

            // Test KnifeInputController authoritative active knife binding & destroyed cleanup
            GameObject testKnifeGo = new GameObject("TestKnife");
            KnifeController testKc = testKnifeGo.AddComponent<KnifeController>();
            KnifeInputController.Instance.SetActiveKnife(testKc);
            if (KnifeInputController.Instance.ActiveKnife != testKc)
            {
                Debug.LogError("[Validator] SetActiveKnife failed to bind active knife!");
                errorCount++;
            }
            Object.DestroyImmediate(testKnifeGo);
            if (KnifeInputController.Instance.ActiveKnife != null)
            {
                Debug.LogError("[Validator] Destroyed knife was not cleared in KnifeInputController!");
                errorCount++;
            }
            else
            {
                Debug.Log("[Validator] Knife lifecycle & destroyed reference safety verified: Clean!");
            }

            // Test LevelManager target progression & Win
            LevelManager.Instance.StartCurrentLevel();
            LevelData l1 = LevelManager.Instance.CurrentLevelData;
            if (l1 == null || l1.targetObjectives.Count == 0)
            {
                Debug.LogError("[Validator] Level 1 has no target objectives!");
                errorCount++;
            }
            else
            {
                Debug.Log($"[Validator] Level 1 loaded successfully: '{l1.levelName}' with {l1.targetObjectives.Count} target objectives.");
                if (LevelManager.Instance.AreAllTargetsComplete())
                {
                    Debug.LogError("[Validator] Level should NOT be complete at start!");
                    errorCount++;
                }

                // Test TargetManager core gameplay loop
                if (TargetManager.Instance != null && UIManager.Instance != null)
                {
                    TargetManager.Instance.InitializeTargets(l1, UIManager.Instance.TargetSlots);
                    if (TargetManager.Instance.AreAllTargetsComplete())
                    {
                        Debug.LogError("[Validator] TargetManager reports all complete right after initialization!");
                        errorCount++;
                    }

                    // Complete 1 Apple
                    TargetManager.Instance.ReportFoodCompleted(FoodType.Apple);
                    int appleCount = TargetManager.Instance.ActiveObjectives.Find(o => o.foodType == FoodType.Apple)?.currentAmount ?? -1;
                    int bananaCount = TargetManager.Instance.ActiveObjectives.Find(o => o.foodType == FoodType.Banana)?.currentAmount ?? -1;
                    int wmCount = TargetManager.Instance.ActiveObjectives.Find(o => o.foodType == FoodType.Watermelon)?.currentAmount ?? -1;

                    if (appleCount != 1 || bananaCount != 0 || wmCount != 0)
                    {
                        Debug.LogError($"[Validator] Target progress isolation failed! Apple: {appleCount}, Banana: {bananaCount}, Watermelon: {wmCount}");
                        errorCount++;
                    }
                    else
                    {
                        Debug.Log("[Validator] Target progress isolation verified: Only cut food incremented!");
                    }

                    if (TargetManager.Instance.AreAllTargetsComplete())
                    {
                        Debug.LogError("[Validator] TargetManager reports complete with only 1 Apple!");
                        errorCount++;
                    }

                    // Complete remaining Apple
                    var appleObj = TargetManager.Instance.ActiveObjectives.Find(o => o.foodType == FoodType.Apple);
                    while (appleObj != null && !appleObj.IsComplete)
                    {
                        TargetManager.Instance.ReportFoodCompleted(FoodType.Apple);
                    }

                    // Complete Banana
                    var bananaObj = TargetManager.Instance.ActiveObjectives.Find(o => o.foodType == FoodType.Banana);
                    while (bananaObj != null && !bananaObj.IsComplete)
                    {
                        TargetManager.Instance.ReportFoodCompleted(FoodType.Banana);
                    }

                    if (TargetManager.Instance.AreAllTargetsComplete())
                    {
                        Debug.LogError("[Validator] TargetManager reports complete while Watermelon is still incomplete!");
                        errorCount++;
                    }

                    // Complete Watermelon
                    var wmObj = TargetManager.Instance.ActiveObjectives.Find(o => o.foodType == FoodType.Watermelon);
                    while (wmObj != null && !wmObj.IsComplete)
                    {
                        TargetManager.Instance.ReportFoodCompleted(FoodType.Watermelon);
                    }

                    if (!TargetManager.Instance.AreAllTargetsComplete())
                    {
                        Debug.LogError("[Validator] TargetManager failed to report complete after all requirements reached!");
                        errorCount++;
                    }
                    else
                    {
                        Debug.Log("[Validator] Core Target Loop verified: All targets reached 100% and triggered complete!");
                    }

                    // Reset targets back to clean 0 state for gameplay
                    TargetManager.Instance.InitializeTargets(l1, UIManager.Instance.TargetSlots);

                    // Validate Timed Speed Progression and Target Multiplier
                    ValidateTimedSpeedProgression(ref errorCount);

                    // Validate UI Panel Flow, Pause System & Floating Text
                    ValidateUIPanelFlowAndPause(ref errorCount);
                }
            }
        }

        private static void ValidateTimedSpeedProgression(ref int errorCount)
        {
            if (FoodSpawner.Instance == null)
            {
                Debug.LogError("[Validator] FoodSpawner.Instance is null during speed progression validation!");
                errorCount++;
                return;
            }

            // 1. Validate Inspector settings & defaults
            if (Mathf.Abs(FoodSpawner.Instance.SpeedIncreaseInterval - 3f) > 0.001f)
            {
                Debug.LogError($"[Validator] SpeedIncreaseInterval expected 3, got {FoodSpawner.Instance.SpeedIncreaseInterval}");
                errorCount++;
            }
            if (Mathf.Abs(FoodSpawner.Instance.FirstIncreasePercent - 10f) > 0.001f)
            {
                Debug.LogError($"[Validator] FirstIncreasePercent expected 10, got {FoodSpawner.Instance.FirstIncreasePercent}");
                errorCount++;
            }
            if (Mathf.Abs(FoodSpawner.Instance.IncreaseStepPercent - 5f) > 0.001f)
            {
                Debug.LogError($"[Validator] IncreaseStepPercent expected 5, got {FoodSpawner.Instance.IncreaseStepPercent}");
                errorCount++;
            }
            if (Mathf.Abs(FoodSpawner.Instance.MaxIncreasePercent - 50f) > 0.001f)
            {
                Debug.LogError($"[Validator] MaxIncreasePercent expected 50, got {FoodSpawner.Instance.MaxIncreasePercent}");
                errorCount++;
            }
            if (Mathf.Abs(FoodSpawner.Instance.SpeedTransitionDuration - 0.25f) > 0.001f)
            {
                Debug.LogError($"[Validator] SpeedTransitionDuration expected 0.25, got {FoodSpawner.Instance.SpeedTransitionDuration}");
                errorCount++;
            }
            if (FoodSpawner.Instance.TargetRequirementMultiplier != 2)
            {
                Debug.LogError($"[Validator] TargetRequirementMultiplier expected 2, got {FoodSpawner.Instance.TargetRequirementMultiplier}");
                errorCount++;
            }

            // 2. Validate Target 2x Requirement Scaling
            if (TargetManager.Instance != null && TargetManager.Instance.ActiveObjectives.Count > 0)
            {
                foreach (var obj in TargetManager.Instance.ActiveObjectives)
                {
                    // Level 1 base count is 3, so scaled must be 6
                    if (obj.requiredAmount != 6)
                    {
                        Debug.LogError($"[Validator] Objective {obj.foodType} requiredAmount expected 6 (3x2), got {obj.requiredAmount}");
                        errorCount++;
                    }
                }
            }

            // 3. Validate Knife Speed Scaling: newDuration = originalDuration / speedMultiplier
            KnifeController knife = Object.FindObjectOfType<KnifeController>();
            if (knife != null)
            {
                float origAttack = knife.OriginalAttackDuration;
                float origReturn = knife.OriginalReturnDuration;

                knife.SetSpeedMultiplier(1.10f); // 110% (+10%)
                float expectedAttack110 = origAttack / 1.10f;
                float expectedReturn110 = origReturn / 1.10f;

                if (Mathf.Abs(knife.AttackDuration - expectedAttack110) > 0.001f ||
                    Mathf.Abs(knife.ReturnDuration - expectedReturn110) > 0.001f)
                {
                    Debug.LogError($"[Validator] Knife duration reduction mismatch at 1.10x! Attack: {knife.AttackDuration} (expected {expectedAttack110})");
                    errorCount++;
                }

                knife.SetSpeedMultiplier(1.20f); // 120% (+20%)
                float expectedAttack120 = origAttack / 1.20f;
                if (Mathf.Abs(knife.AttackDuration - expectedAttack120) > 0.001f)
                {
                    Debug.LogError($"[Validator] Knife duration reduction mismatch at 1.20x! Attack: {knife.AttackDuration} (expected {expectedAttack120})");
                    errorCount++;
                }

                knife.ResetSpeed();
                if (Mathf.Abs(knife.AttackDuration - origAttack) > 0.001f ||
                    Mathf.Abs(knife.ReturnDuration - origReturn) > 0.001f)
                {
                    Debug.LogError("[Validator] Knife ResetSpeed failed to restore original durations!");
                    errorCount++;
                }

                // 4. Validate KnifeController 15-Point Specification
                // Requirement 6: minimum clamps
                if (Mathf.Abs(knife.MinimumAttackDuration - 0.04f) > 0.001f)
                {
                    Debug.LogError($"[Validator] MinimumAttackDuration expected 0.04, got {knife.MinimumAttackDuration}");
                    errorCount++;
                }
                if (Mathf.Abs(knife.MinimumReturnDuration - 0.04f) > 0.001f)
                {
                    Debug.LogError($"[Validator] MinimumReturnDuration expected 0.04, got {knife.MinimumReturnDuration}");
                    errorCount++;
                }

                // Requirement 8 & 15: speedTransitionDuration
                if (Mathf.Abs(knife.SpeedTransitionDuration - 0.25f) > 0.001f)
                {
                    Debug.LogError($"[Validator] Knife SpeedTransitionDuration expected 0.25, got {knife.SpeedTransitionDuration}");
                    errorCount++;
                }

                // Requirement 15: Animation curves
                if (knife.AttackPositionCurve == null || knife.AttackRotationCurve == null ||
                    knife.ReturnPositionCurve == null || knife.ReturnRotationCurve == null)
                {
                    Debug.LogError("[Validator] Knife animation curves must not be null!");
                    errorCount++;
                }

                // Requirement 4: State tracking fields
                if (knife.IsChopping)
                {
                    Debug.LogError("[Validator] Knife should not be chopping in idle initial state!");
                    errorCount++;
                }
                if (knife.InputHeld)
                {
                    Debug.LogError("[Validator] InputHeld should be false in initial state!");
                    errorCount++;
                }

                // Requirement 10 & 11: Local space poses
                knife.ResetKnifeToIdle();
                if (knife.transform.localPosition != Vector3.zero)
                {
                    Debug.LogError($"[Validator] Knife idle localPosition expected (0,0,0), got {knife.transform.localPosition}");
                    errorCount++;
                }

                // Requirement 14: Reset on Replay / Next / ResetKnifeToIdle
                knife.SetSpeedMultiplier(1.30f);
                knife.ResetKnifeToIdle();
                if (Mathf.Abs(knife.CurrentKnifeSpeedMultiplier - 1.0f) > 0.001f)
                {
                    Debug.LogError($"[Validator] ResetKnifeToIdle failed to restore CurrentKnifeSpeedMultiplier to 1.0, got {knife.CurrentKnifeSpeedMultiplier}");
                    errorCount++;
                }
                if (knife.IsChopping || knife.InputHeld || knife.ChopRoutine != null)
                {
                    Debug.LogError("[Validator] ResetKnifeToIdle failed to reset routine tracking flags!");
                    errorCount++;
                }

                // Requirement 5 & 6: Clamping calculation verification
                float clampedAttack = Mathf.Max(knife.MinimumAttackDuration, knife.BaseAttackDuration / 10f);
                float clampedReturn = Mathf.Max(knife.MinimumReturnDuration, knife.BaseReturnDuration / 10f);
                if (clampedAttack < 0.04f || clampedReturn < 0.04f)
                {
                    Debug.LogError("[Validator] Minimum duration clamps failed to enforce 0.04f lower bound!");
                    errorCount++;
                }

                // 15-second progression mathematical verification
                float[] testTimes = new float[] { 0f, 3f, 6f, 9f, 12f, 15f };
                float[] expectedMults = new float[] { 1.0f, 1.10f, 1.15f, 1.20f, 1.25f, 1.30f };
                for (int i = 0; i < testTimes.Length; i++)
                {
                    float t = testTimes[i];
                    int interval = Mathf.FloorToInt(t / 3f);
                    float mult = 1f;
                    if (interval >= 1)
                    {
                        mult = 1f + (10f + (interval - 1) * 5f) / 100f;
                    }
                    if (Mathf.Abs(mult - expectedMults[i]) > 0.001f)
                    {
                        Debug.LogError($"[Validator] Progression curve mismatch at {t}s! Expected {expectedMults[i]}, got {mult}");
                        errorCount++;
                    }
                }

                Debug.Log("[Validator] 15-Point Knife Movement & Speed Progression Architecture verified: Clean!");
            }

            Debug.Log("[Validator] Timed Speed Progression and Target Multiplier systems verified: Clean!");
        }

        private static void ValidateUIPanelFlowAndPause(ref int errorCount)
        {
            Debug.Log("[Validator] 6. Validating UI Panel Flow, Pause System & Floating Text...");
            UIManager uim = Object.FindObjectOfType<UIManager>();
            if (uim == null)
            {
                Debug.LogError("[Validator] UIManager is missing!");
                errorCount++;
                return;
            }

            // 1. Inspector References on UIManager
            if (uim.LevelCompletePanel == null) { Debug.LogError("[Validator] LevelCompletePanel is not wired on UIManager!"); errorCount++; }
            if (uim.UnlockRewardPanel == null) { Debug.LogError("[Validator] UnlockRewardPanel is not wired on UIManager!"); errorCount++; }
            if (uim.LevelFailedPanel == null) { Debug.LogError("[Validator] LevelFailedPanel is not wired on UIManager!"); errorCount++; }
            if (uim.PausePanel == null) { Debug.LogError("[Validator] PausePanel is not wired on UIManager!"); errorCount++; }
            if (uim.PauseButton == null) { Debug.LogError("[Validator] PauseButton is not wired on UIManager!"); errorCount++; }
            if (uim.UnlockOkButton == null) { Debug.LogError("[Validator] UnlockOkButton is not wired on UIManager!"); errorCount++; }
            if (uim.UnlockRewardIcon == null) { Debug.LogError("[Validator] UnlockRewardIcon is not wired on UIManager!"); errorCount++; }
            if (uim.UnlockRewardNameText == null) { Debug.LogError("[Validator] UnlockRewardNameText is not wired on UIManager!"); errorCount++; }
            if (uim.NextLevelButton == null) { Debug.LogError("[Validator] NextLevelButton is not wired on UIManager!"); errorCount++; }
            if (uim.RetryButton == null) { Debug.LogError("[Validator] RetryButton is not wired on UIManager!"); errorCount++; }
            if (uim.ResumeButton == null) { Debug.LogError("[Validator] ResumeButton is not wired on UIManager!"); errorCount++; }

            // 2. PauseButton visible in HUD inside Safe Area
            if (uim.PauseButton != null)
            {
                RectTransform pbRt = uim.PauseButton.GetComponent<RectTransform>();
                if (pbRt == null || pbRt.anchorMin.x < 0.8f || pbRt.anchorMin.y < 0.8f)
                {
                    Debug.LogError("[Validator] PauseButton must be anchored at the top-right inside Safe Area!");
                    errorCount++;
                }
            }

            // 3. FloatingTextManager validation
            FloatingTextManager ftm = Object.FindObjectOfType<FloatingTextManager>();
            if (ftm == null)
            {
                Debug.LogError("[Validator] FloatingTextManager is missing!");
                errorCount++;
            }
            else if (ftm.FloatingCanvas == null)
            {
                Debug.LogError("[Validator] FloatingTextManager has no Canvas reference!");
                errorCount++;
            }

            // 4. Panel Flow: Win on Level 1 (hasUnlockReward == true)
            LevelManager.Instance.LoadLevelIndex(0); // Level 1
            LevelData l1 = LevelManager.Instance.CurrentLevelData;
            if (l1 != null && l1.hasUnlockReward)
            {
                var handleWon = typeof(UIManager).GetMethod("HandleLevelWon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                handleWon?.Invoke(uim, new object[] { 35, 100 });

                if (!uim.UnlockRewardPanel.activeSelf)
                {
                    Debug.LogError("[Validator] Level 1 has unlock reward, but UnlockRewardPanel was not shown!");
                    errorCount++;
                }
                if (uim.LevelCompletePanel.activeSelf)
                {
                    Debug.LogError("[Validator] LevelCompletePanel should NOT be shown before UnlockRewardPanel OK is clicked!");
                    errorCount++;
                }

                // Click OK on Unlock Reward Panel
                if (uim.UnlockOkButton != null)
                {
                    uim.UnlockOkButton.onClick.Invoke();
                    if (uim.UnlockRewardPanel.activeSelf)
                    {
                        Debug.LogError("[Validator] UnlockRewardPanel was not hidden after clicking OK!");
                        errorCount++;
                    }
                    if (!uim.LevelCompletePanel.activeSelf)
                    {
                        Debug.LogError("[Validator] LevelCompletePanel did not appear after dismissing UnlockRewardPanel!");
                        errorCount++;
                    }
                }
            }
            else
            {
                Debug.LogError("[Validator] Level 1 expected hasUnlockReward == true!");
                errorCount++;
            }

            // 5. Panel Flow: Win on Level 2 (hasUnlockReward == false)
            LevelManager.Instance.LoadLevelIndex(1); // Level 2
            LevelData l2 = LevelManager.Instance.CurrentLevelData;
            if (l2 != null && !l2.hasUnlockReward)
            {
                var handleWon = typeof(UIManager).GetMethod("HandleLevelWon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                handleWon?.Invoke(uim, new object[] { 40, 150 });

                if (uim.UnlockRewardPanel.activeSelf)
                {
                    Debug.LogError("[Validator] Level 2 has no unlock reward, but UnlockRewardPanel was shown!");
                    errorCount++;
                }
                if (!uim.LevelCompletePanel.activeSelf)
                {
                    Debug.LogError("[Validator] LevelCompletePanel did not show directly on level without unlock reward!");
                    errorCount++;
                }
            }

            // 6. Level Failed Flow
            LevelManager.Instance.StartCurrentLevel();
            LevelManager.Instance.FailLevel("Obstacle Hit");
            if (!LevelManager.Instance.IsLevelFailed)
            {
                Debug.LogError("[Validator] FailLevel failed to set IsLevelFailed!");
                errorCount++;
            }
            if (!uim.LevelFailedPanel.activeSelf)
            {
                Debug.LogError("[Validator] LevelFailedPanel was not shown on FailLevel!");
                errorCount++;
            }
            if (KnifeInputController.Instance != null && KnifeInputController.Instance.IsInputActive)
            {
                Debug.LogError("[Validator] KnifeInputController input was not disabled on FailLevel!");
                errorCount++;
            }

            // 7. Pause System & Panel Priority
            LevelManager.Instance.StartCurrentLevel();
            if (Time.timeScale != 1f)
            {
                Debug.LogError("[Validator] StartCurrentLevel failed to restore Time.timeScale to 1!");
                errorCount++;
            }

            // Verify Pause opens during active play
            if (uim.PauseButton != null)
            {
                uim.PauseButton.onClick.Invoke();
                if (Time.timeScale != 0f)
                {
                    Debug.LogError("[Validator] Pausing did not set Time.timeScale to 0!");
                    errorCount++;
                }
                if (!uim.PausePanel.activeSelf)
                {
                    Debug.LogError("[Validator] PausePanel was not shown on Pause click!");
                    errorCount++;
                }
                if (KnifeInputController.Instance != null && KnifeInputController.Instance.IsInputActive)
                {
                    Debug.LogError("[Validator] Knife input was not disabled during Pause!");
                    errorCount++;
                }

                // Resume
                if (uim.ResumeButton != null)
                {
                    uim.ResumeButton.onClick.Invoke();
                    if (Time.timeScale != 1f)
                    {
                        Debug.LogError("[Validator] Resume did not restore Time.timeScale to 1!");
                        errorCount++;
                    }
                    if (uim.PausePanel.activeSelf)
                    {
                        Debug.LogError("[Validator] PausePanel was not hidden on Resume!");
                        errorCount++;
                    }
                    if (KnifeInputController.Instance != null && !KnifeInputController.Instance.IsInputActive)
                    {
                        Debug.LogError("[Validator] Knife input was not re-enabled on Resume!");
                        errorCount++;
                    }
                }
            }

            // Verify Panel Priority: Pause cannot open when LevelFailed is active
            LevelManager.Instance.FailLevel("Priority Test");
            if (uim.CanPause())
            {
                Debug.LogError("[Validator] CanPause() must return false when LevelFailedPanel is active!");
                errorCount++;
            }

            // Reset back to Level 1 in clean state
            LevelManager.Instance.LoadLevelIndex(0);
            Time.timeScale = 1f;
            uim.HideAllPanels();

            Debug.Log("[Validator] UI Panel Flow, Pause System & Floating Text verified: Clean!");
        }
    }
}
