using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FoodSlicer
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD Elements")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text multiplierText;
        [SerializeField] private Text levelTitleText;
        [SerializeField] private ComboMeterUI comboMeter;
        [SerializeField] private List<TargetSlotUI> targetSlots = new List<TargetSlotUI>();
        [SerializeField] private Button pauseButton;

        [Header("Level Complete Panel")]
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private Text completeScoreText;
        [SerializeField] private Text completeCoinsText;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button completeReplayButton;
        [SerializeField] private Button completeHomeButton;

        [Header("Unlock Reward Panel")]
        [SerializeField] private GameObject unlockRewardPanel;
        [SerializeField] private Image unlockRewardIcon;
        [SerializeField] private Text unlockRewardNameText;
        [SerializeField] private Text unlockRewardTitleText;
        [SerializeField] private Button unlockOkButton;

        [Header("Level Failed Panel")]
        [SerializeField] private GameObject levelFailedPanel;
        [SerializeField] private Text failReasonText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button failHomeButton;

        [Header("Pause Panel")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button pauseHomeButton;

        public List<TargetSlotUI> TargetSlots => targetSlots;
        public float TargetProgressAnimationDuration => 0.20f;

        public GameObject LevelCompletePanel => levelCompletePanel;
        public GameObject UnlockRewardPanel => unlockRewardPanel;
        public GameObject LevelFailedPanel => levelFailedPanel;
        public GameObject PausePanel => pausePanel;
        public Button PauseButton => pauseButton;
        public Button UnlockOkButton => unlockOkButton;
        public Button ResumeButton => resumeButton;
        public Button RetryButton => retryButton;
        public Button NextLevelButton => nextLevelButton;
        public Image UnlockRewardIcon => unlockRewardIcon;
        public Text UnlockRewardNameText => unlockRewardNameText;
        public Text FailReasonText => failReasonText;

        private Coroutine activePopupRoutine;
        private int cachedCoinsEarned = 50;
        private int cachedFinalScore = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            WireButtons();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void WireButtons()
        {
            if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
            if (pauseHomeButton != null) pauseHomeButton.onClick.AddListener(OnHomeClicked);

            if (nextLevelButton != null) nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            if (completeReplayButton != null) completeReplayButton.onClick.AddListener(OnRestartClicked);
            if (completeHomeButton != null) completeHomeButton.onClick.AddListener(OnHomeClicked);

            if (unlockOkButton != null) unlockOkButton.onClick.AddListener(OnUnlockOkClicked);

            if (retryButton != null) retryButton.onClick.AddListener(OnRestartClicked);
            if (failHomeButton != null) failHomeButton.onClick.AddListener(OnHomeClicked);
        }

        private void OnEnable()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
            }
            if (ComboManager.Instance != null)
            {
                ComboManager.Instance.OnMultiplierChanged += HandleMultiplierChanged;
            }
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnLevelStarted += HandleLevelStarted;
                LevelManager.Instance.OnTargetUpdated += HandleTargetUpdated;
                LevelManager.Instance.OnLevelWon += HandleLevelWon;
                LevelManager.Instance.OnLevelFailed += HandleLevelFailed;
            }
        }

        private void OnDisable()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
            }
            if (ComboManager.Instance != null)
            {
                ComboManager.Instance.OnMultiplierChanged -= HandleMultiplierChanged;
            }
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnLevelStarted -= HandleLevelStarted;
                LevelManager.Instance.OnTargetUpdated -= HandleTargetUpdated;
                LevelManager.Instance.OnLevelWon -= HandleLevelWon;
                LevelManager.Instance.OnLevelFailed -= HandleLevelFailed;
            }
        }

        private void Start()
        {
            HideAllPanels();
            UpdateScoreUI(0);
            UpdateMultiplierUI(1);
        }

        public void HideAllPanels()
        {
            if (activePopupRoutine != null)
            {
                StopCoroutine(activePopupRoutine);
                activePopupRoutine = null;
            }

            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
            if (unlockRewardPanel != null) unlockRewardPanel.SetActive(false);
            if (levelFailedPanel != null) levelFailedPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        private void HandleScoreChanged(int totalScore)
        {
            UpdateScoreUI(totalScore);
        }

        private void UpdateScoreUI(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"{score}";
            }
        }

        private void HandleMultiplierChanged(int mult)
        {
            UpdateMultiplierUI(mult);
        }

        private void UpdateMultiplierUI(int mult)
        {
            if (multiplierText != null)
            {
                multiplierText.text = $"x{mult}";
            }
        }

        private void HandleLevelStarted(LevelData level)
        {
            Time.timeScale = 1f;
            HideAllPanels();

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(true);
            }

            if (levelTitleText != null && level != null)
            {
                levelTitleText.text = $"LEVEL {level.levelNumber}";
            }

            // Setup Target Slots via TargetManager
            if (TargetManager.Instance != null && level != null)
            {
                TargetManager.Instance.InitializeTargets(level, targetSlots);
            }
        }

        private void HandleTargetUpdated(FoodType foodType, int current, int required)
        {
            if (targetSlots == null) return;

            foreach (var slot in targetSlots)
            {
                if (slot != null && slot.IsConfigured && slot.SlotFoodType == foodType)
                {
                    slot.UpdateProgress(current);
                }
            }
        }

        // ----------------------------------------------------
        // COMPLETION & REWARD FLOW
        // ----------------------------------------------------
        private void HandleLevelWon(int earnedCoins, int finalScore)
        {
            cachedCoinsEarned = earnedCoins;
            cachedFinalScore = finalScore;

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(false);
            }

            LevelData level = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevelData : null;
            if (level != null && level.hasUnlockReward)
            {
                ShowUnlockRewardPanel(level);
            }
            else
            {
                ShowLevelCompletePanel();
            }
        }

        private void ShowUnlockRewardPanel(LevelData level)
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
            if (levelFailedPanel != null) levelFailedPanel.SetActive(false);

            if (unlockRewardPanel != null)
            {
                if (unlockRewardTitleText != null)
                {
                    unlockRewardTitleText.text = "UNLOCKED!";
                }

                if (unlockRewardIcon != null && level.unlockRewardIcon != null)
                {
                    unlockRewardIcon.sprite = level.unlockRewardIcon;
                    unlockRewardIcon.gameObject.SetActive(true);
                }

                if (unlockRewardNameText != null)
                {
                    unlockRewardNameText.text = !string.IsNullOrEmpty(level.unlockRewardName)
                        ? level.unlockRewardName
                        : "New Knife Unlocked!";
                }

                unlockRewardPanel.transform.SetAsLastSibling();
                unlockRewardPanel.SetActive(true);

                if (activePopupRoutine != null) StopCoroutine(activePopupRoutine);
                activePopupRoutine = StartCoroutine(AnimatePanelPopup(unlockRewardPanel));
            }
            else
            {
                // Fallback to complete panel directly
                ShowLevelCompletePanel();
            }
        }

        private void OnUnlockOkClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (unlockRewardPanel != null) unlockRewardPanel.SetActive(false);

            ShowLevelCompletePanel();
        }

        private void ShowLevelCompletePanel()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (unlockRewardPanel != null) unlockRewardPanel.SetActive(false);
            if (levelFailedPanel != null) levelFailedPanel.SetActive(false);

            if (completeScoreText != null)
            {
                completeScoreText.text = $"SCORE: {cachedFinalScore}";
            }
            if (completeCoinsText != null)
            {
                completeCoinsText.text = $"+{cachedCoinsEarned} COINS";
            }

            if (levelCompletePanel != null)
            {
                levelCompletePanel.transform.SetAsLastSibling();
                levelCompletePanel.SetActive(true);

                if (activePopupRoutine != null) StopCoroutine(activePopupRoutine);
                activePopupRoutine = StartCoroutine(AnimatePanelPopup(levelCompletePanel));
            }
        }

        // ----------------------------------------------------
        // LEVEL FAILED FLOW
        // ----------------------------------------------------
        private void HandleLevelFailed(string reason)
        {
            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(false);
            }

            if (pausePanel != null) pausePanel.SetActive(false);
            if (unlockRewardPanel != null) unlockRewardPanel.SetActive(false);
            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);

            if (failReasonText != null)
            {
                failReasonText.text = !string.IsNullOrEmpty(reason) ? reason : "Level Failed!";
            }

            if (levelFailedPanel != null)
            {
                levelFailedPanel.transform.SetAsLastSibling();
                levelFailedPanel.SetActive(true);

                if (activePopupRoutine != null) StopCoroutine(activePopupRoutine);
                activePopupRoutine = StartCoroutine(AnimatePanelPopup(levelFailedPanel));
            }
        }

        // ----------------------------------------------------
        // PAUSE SYSTEM & PANEL PRIORITY
        // ----------------------------------------------------
        public bool IsMajorPanelActive()
        {
            return (levelFailedPanel != null && levelFailedPanel.activeSelf) ||
                   (unlockRewardPanel != null && unlockRewardPanel.activeSelf) ||
                   (levelCompletePanel != null && levelCompletePanel.activeSelf);
        }

        public bool CanPause()
        {
            if (IsMajorPanelActive()) return false;

            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Playing)
            {
                return false;
            }

            if (LevelManager.Instance != null && (LevelManager.Instance.IsLevelWon || LevelManager.Instance.IsLevelFailed))
            {
                return false;
            }

            return true;
        }

        private void OnPauseClicked()
        {
            if (!CanPause()) return;

            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

            Time.timeScale = 0f;
            if (KnifeInputController.Instance != null) KnifeInputController.Instance.SetInputActive(false);
            if (GameStateManager.Instance != null) GameStateManager.Instance.SetState(GameState.Paused);

            if (pauseButton != null) pauseButton.gameObject.SetActive(false);

            if (pausePanel != null)
            {
                pausePanel.transform.SetAsLastSibling();
                pausePanel.SetActive(true);

                if (activePopupRoutine != null) StopCoroutine(activePopupRoutine);
                activePopupRoutine = StartCoroutine(AnimatePanelPopup(pausePanel));
            }
        }

        private void OnResumeClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

            if (pausePanel != null) pausePanel.SetActive(false);

            Time.timeScale = 1f;
            if (KnifeInputController.Instance != null) KnifeInputController.Instance.SetInputActive(true);
            if (GameStateManager.Instance != null) GameStateManager.Instance.SetState(GameState.Playing);

            if (pauseButton != null) pauseButton.gameObject.SetActive(true);
        }

        private void OnRestartClicked()
        {
            Time.timeScale = 1f;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            HideAllPanels();
            if (LevelManager.Instance != null) LevelManager.Instance.ReplayLevel();
        }

        private void OnNextLevelClicked()
        {
            Time.timeScale = 1f;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            HideAllPanels();
            if (LevelManager.Instance != null) LevelManager.Instance.NextLevel();
        }

        private void OnHomeClicked()
        {
            Time.timeScale = 1f;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            HideAllPanels();
            if (LevelManager.Instance != null) LevelManager.Instance.ReturnToMainMenu();
        }

        // ----------------------------------------------------
        // PROFESSIONAL PANEL ENTRANCE ANIMATION (UNSCALED TIME)
        // ----------------------------------------------------
        private IEnumerator AnimatePanelPopup(GameObject panel)
        {
            if (panel == null) yield break;

            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();

            // Block clicks through, but disable panel button interaction during entrance
            cg.interactable = false;
            cg.blocksRaycasts = true;
            cg.alpha = 0f;

            Transform card = panel.transform.Find("Card");
            Transform scaleTarget = card != null ? card : panel.transform;

            Vector3 startScale = new Vector3(0.88f, 0.88f, 0.88f);
            Vector3 peakScale = new Vector3(1.04f, 1.04f, 1.04f);
            Vector3 endScale = Vector3.one;

            if (scaleTarget != null) scaleTarget.localScale = startScale;

            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Alpha fade 0 -> 1
                cg.alpha = t;

                // Scale pop: 0.88 -> 1.04 -> 1.00
                if (scaleTarget != null)
                {
                    scaleTarget.localScale = t < 0.70f
                        ? Vector3.Lerp(startScale, peakScale, t / 0.70f)
                        : Vector3.Lerp(peakScale, endScale, (t - 0.70f) / 0.30f);
                }

                yield return null;
            }

            if (scaleTarget != null) scaleTarget.localScale = endScale;
            cg.alpha = 1f;

            // Buttons become interactable ONLY after entrance animation finishes
            cg.interactable = true;
            cg.blocksRaycasts = true;
            activePopupRoutine = null;
        }
    }
}
