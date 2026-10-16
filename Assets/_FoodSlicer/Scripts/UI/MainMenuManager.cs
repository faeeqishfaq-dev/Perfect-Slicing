using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FoodSlicer
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Menu UI References")]
        [SerializeField] private Text coinsDisplayText;
        [SerializeField] private Text levelDisplayText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button knivesShopButton;
        [SerializeField] private Button settingsButton;

        [Header("Settings Panel References")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle hapticsToggle;
        [SerializeField] private Button closeSettingsButton;

        [Header("Knife Shop Reference")]
        [SerializeField] private KnifeShopManager knifeShopManager;

        [Header("Showcase Pedestal (Rotating Preview)")]
        [SerializeField] private Transform showcaseKnifeRoot;
        [SerializeField] private float pedestalRotationSpeed = 25f;

        private GameObject activeShowcaseKnife;

        private void Start()
        {
            WireButtons();
            RefreshMenuDisplay();

            if (settingsPanel != null) settingsPanel.SetActive(false);

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnCoinsChanged += HandleCoinsChanged;
                SaveManager.Instance.OnKnifeSelected += HandleKnifeSelected;
            }
        }

        private void OnDestroy()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
                SaveManager.Instance.OnKnifeSelected -= HandleKnifeSelected;
            }
        }

        private void Update()
        {
            if (showcaseKnifeRoot != null)
            {
                showcaseKnifeRoot.Rotate(Vector3.up * (pedestalRotationSpeed * Time.deltaTime));
            }
        }

        private void WireButtons()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (knivesShopButton != null) knivesShopButton.onClick.AddListener(OnKnivesShopClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(OnCloseSettingsClicked);

            if (soundToggle != null)
            {
                soundToggle.isOn = SaveManager.Instance == null || SaveManager.Instance.IsSoundEnabled();
                soundToggle.onValueChanged.AddListener(OnSoundToggled);
            }

            if (musicToggle != null)
            {
                musicToggle.isOn = SaveManager.Instance == null || SaveManager.Instance.IsMusicEnabled();
                musicToggle.onValueChanged.AddListener(OnMusicToggled);
            }

            if (hapticsToggle != null)
            {
                hapticsToggle.isOn = SaveManager.Instance == null || SaveManager.Instance.IsHapticsEnabled();
                hapticsToggle.onValueChanged.AddListener(OnHapticsToggled);
            }
        }

        public void RefreshMenuDisplay()
        {
            if (coinsDisplayText != null && SaveManager.Instance != null)
            {
                coinsDisplayText.text = $"🪙 {SaveManager.Instance.GetCoins()}";
            }

            if (levelDisplayText != null && SaveManager.Instance != null)
            {
                levelDisplayText.text = $"LEVEL {SaveManager.Instance.GetUnlockedLevel()}";
            }

            UpdateShowcaseKnife();
        }

        private void HandleCoinsChanged(int newCoins)
        {
            if (coinsDisplayText != null)
            {
                coinsDisplayText.text = $"🪙 {newCoins}";
            }
        }

        private void HandleKnifeSelected(int knifeIndex)
        {
            UpdateShowcaseKnife();
        }

        private void UpdateShowcaseKnife()
        {
            if (showcaseKnifeRoot == null || knifeShopManager == null) return;

            if (activeShowcaseKnife != null)
            {
                Destroy(activeShowcaseKnife);
            }

            KnifeData selected = knifeShopManager.GetSelectedKnifeData();
            if (selected != null && selected.knifePrefab != null)
            {
                activeShowcaseKnife = Instantiate(selected.knifePrefab, showcaseKnifeRoot.position, showcaseKnifeRoot.rotation, showcaseKnifeRoot);
                KnifeController kc = activeShowcaseKnife.GetComponent<KnifeController>();
                if (kc != null) Destroy(kc);
                Collider col = activeShowcaseKnife.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
        }

        private void OnPlayClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (GameStateManager.Instance != null) GameStateManager.Instance.SetState(GameState.Loading);
            SceneManager.LoadScene("Gameplay");
        }

        private void OnKnivesShopClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (knifeShopManager != null) knifeShopManager.OpenShop();
        }

        private void OnSettingsClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        private void OnCloseSettingsClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void OnSoundToggled(bool value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetSoundEnabled(value);
        }

        private void OnMusicToggled(bool value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicEnabled(value);
        }

        private void OnHapticsToggled(bool value)
        {
            if (HapticManager.Instance != null) HapticManager.Instance.SetHapticsEnabled(value);
        }
    }
}
