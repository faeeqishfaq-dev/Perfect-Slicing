using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FoodSlicer
{
    public class KnifeShopManager : MonoBehaviour
    {
        public static KnifeShopManager Instance { get; private set; }

        [Header("Knife Database (6 Knives)")]
        [SerializeField] private List<KnifeData> knifeDatabase = new List<KnifeData>();

        [Header("UI References")]
        [SerializeField] private Text knifeNameText;
        [SerializeField] private Text knifePriceText;
        [SerializeField] private Text coinsDisplayText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject equippedBadge;
        [SerializeField] private Button nextKnifeButton;
        [SerializeField] private Button prevKnifeButton;
        [SerializeField] private Button backButton;
        [SerializeField] private GameObject shopPanel;

        [Header("3D Showcase Pedestal")]
        [SerializeField] private Transform showcaseSpawnPoint;
        private GameObject currentPreviewInstance;

        private int viewingIndex = 0;

        public List<KnifeData> KnifeDatabase => knifeDatabase;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (knifeDatabase == null || knifeDatabase.Count == 0)
            {
                var loaded = Resources.LoadAll<KnifeData>("Knives");
                if (loaded != null && loaded.Length > 0)
                {
                    knifeDatabase = new List<KnifeData>(loaded);
                    knifeDatabase.Sort((a, b) => a.knifeIndex.CompareTo(b.knifeIndex));
                }
            }

            WireButtons();
        }

        private void Start()
        {
            int selected = SaveManager.Instance != null ? SaveManager.Instance.GetSelectedKnife() : 0;
            viewingIndex = Mathf.Clamp(selected, 0, knifeDatabase.Count - 1);
            UpdateCoinsDisplay();
            RefreshShopUI();
        }

        private void WireButtons()
        {
            if (nextKnifeButton != null) nextKnifeButton.onClick.AddListener(NextKnife);
            if (prevKnifeButton != null) prevKnifeButton.onClick.AddListener(PrevKnife);
            if (buyButton != null) buyButton.onClick.AddListener(BuyCurrentKnife);
            if (selectButton != null) selectButton.onClick.AddListener(SelectCurrentKnife);
            if (backButton != null) backButton.onClick.AddListener(CloseShop);
        }

        public void OpenShop()
        {
            if (shopPanel != null) shopPanel.SetActive(true);
            int selected = SaveManager.Instance != null ? SaveManager.Instance.GetSelectedKnife() : 0;
            viewingIndex = Mathf.Clamp(selected, 0, knifeDatabase.Count - 1);
            UpdateCoinsDisplay();
            RefreshShopUI();
        }

        public void CloseShop()
        {
            if (shopPanel != null) shopPanel.SetActive(false);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        }

        public void NextKnife()
        {
            if (knifeDatabase.Count == 0) return;
            viewingIndex = (viewingIndex + 1) % knifeDatabase.Count;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            RefreshShopUI();
        }

        public void PrevKnife()
        {
            if (knifeDatabase.Count == 0) return;
            viewingIndex = (viewingIndex - 1 + knifeDatabase.Count) % knifeDatabase.Count;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            RefreshShopUI();
        }

        public void RefreshShopUI()
        {
            if (knifeDatabase == null || knifeDatabase.Count == 0) return;
            viewingIndex = Mathf.Clamp(viewingIndex, 0, knifeDatabase.Count - 1);

            KnifeData knife = knifeDatabase[viewingIndex];
            if (knife == null) return;

            if (knifeNameText != null) knifeNameText.text = knife.knifeName;

            bool isOwned = SaveManager.Instance != null && SaveManager.Instance.IsKnifeOwned(viewingIndex);
            int selectedIndex = SaveManager.Instance != null ? SaveManager.Instance.GetSelectedKnife() : 0;
            bool isSelected = (viewingIndex == selectedIndex);

            if (knifePriceText != null)
            {
                knifePriceText.text = isOwned ? "OWNED" : $"🪙 {knife.price}";
            }

            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(!isOwned);
                int coins = SaveManager.Instance != null ? SaveManager.Instance.GetCoins() : 0;
                buyButton.interactable = (coins >= knife.price);
            }

            if (selectButton != null)
            {
                selectButton.gameObject.SetActive(isOwned && !isSelected);
            }

            if (equippedBadge != null)
            {
                equippedBadge.SetActive(isSelected);
            }

            UpdatePreviewModel(knife);
            UpdateCoinsDisplay();
        }

        private void UpdatePreviewModel(KnifeData knife)
        {
            if (showcaseSpawnPoint == null) return;

            if (currentPreviewInstance != null)
            {
                Destroy(currentPreviewInstance);
            }

            if (knife.knifePrefab != null)
            {
                currentPreviewInstance = Instantiate(knife.knifePrefab, showcaseSpawnPoint.position, showcaseSpawnPoint.rotation, showcaseSpawnPoint);
                // Disable knife controller / physics for shop showcase
                KnifeController kc = currentPreviewInstance.GetComponent<KnifeController>();
                if (kc != null) Destroy(kc);
                Collider col = currentPreviewInstance.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
        }

        public void BuyCurrentKnife()
        {
            if (knifeDatabase.Count == 0) return;
            KnifeData knife = knifeDatabase[viewingIndex];
            if (knife == null) return;

            if (SaveManager.Instance != null)
            {
                bool success = SaveManager.Instance.SpendCoins(knife.price);
                if (success)
                {
                    SaveManager.Instance.SetKnifeOwned(viewingIndex, true);
                    SaveManager.Instance.SetSelectedKnife(viewingIndex);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayCoinGain();
                    RefreshShopUI();
                }
            }
        }

        public void SelectCurrentKnife()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SetSelectedKnife(viewingIndex);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                RefreshShopUI();
            }
        }

        private void UpdateCoinsDisplay()
        {
            if (coinsDisplayText != null && SaveManager.Instance != null)
            {
                coinsDisplayText.text = $"🪙 {SaveManager.Instance.GetCoins()}";
            }
        }

        public KnifeData GetSelectedKnifeData()
        {
            int selected = SaveManager.Instance != null ? SaveManager.Instance.GetSelectedKnife() : 0;
            selected = Mathf.Clamp(selected, 0, knifeDatabase.Count - 1);
            return (knifeDatabase != null && knifeDatabase.Count > selected) ? knifeDatabase[selected] : null;
        }
    }
}
