using System;
using UnityEngine;

namespace FoodSlicer
{
    [DefaultExecutionOrder(-100)]
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string KEY_COINS = "FS_Coins";
        private const string KEY_UNLOCKED_LEVEL = "FS_UnlockedLevel";
        private const string KEY_SELECTED_KNIFE = "FS_SelectedKnife";
        private const string KEY_OWNED_KNIFE_PREFIX = "FS_OwnedKnife_";
        private const string KEY_SOUND_ENABLED = "FS_SoundEnabled";
        private const string KEY_MUSIC_ENABLED = "FS_MusicEnabled";
        private const string KEY_HAPTICS_ENABLED = "FS_HapticsEnabled";

        [Header("Debug Settings")]
        [SerializeField] [Tooltip("Initial coins granted to new players")]
        private int startingCoins = 100;

        public event Action<int> OnCoinsChanged;
        public event Action<int> OnKnifeSelected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            InitializeDefaults();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void InitializeDefaults()
        {
            if (!PlayerPrefs.HasKey(KEY_COINS))
            {
                PlayerPrefs.SetInt(KEY_COINS, startingCoins);
            }

            if (!PlayerPrefs.HasKey(KEY_UNLOCKED_LEVEL))
            {
                PlayerPrefs.SetInt(KEY_UNLOCKED_LEVEL, 1);
            }

            if (!PlayerPrefs.HasKey(KEY_SELECTED_KNIFE))
            {
                PlayerPrefs.SetInt(KEY_SELECTED_KNIFE, 0);
            }

            // Knife 0 is always owned by default
            SetKnifeOwned(0, true);

            if (!PlayerPrefs.HasKey(KEY_SOUND_ENABLED))
            {
                PlayerPrefs.SetInt(KEY_SOUND_ENABLED, 1);
            }

            if (!PlayerPrefs.HasKey(KEY_MUSIC_ENABLED))
            {
                PlayerPrefs.SetInt(KEY_MUSIC_ENABLED, 1);
            }

            if (!PlayerPrefs.HasKey(KEY_HAPTICS_ENABLED))
            {
                PlayerPrefs.SetInt(KEY_HAPTICS_ENABLED, 1);
            }

            PlayerPrefs.Save();
        }

        public int GetCoins()
        {
            return PlayerPrefs.GetInt(KEY_COINS, startingCoins);
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            int total = GetCoins() + amount;
            PlayerPrefs.SetInt(KEY_COINS, total);
            PlayerPrefs.Save();
            OnCoinsChanged?.Invoke(total);
        }

        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return true;
            int current = GetCoins();
            if (current >= amount)
            {
                int remaining = current - amount;
                PlayerPrefs.SetInt(KEY_COINS, remaining);
                PlayerPrefs.Save();
                OnCoinsChanged?.Invoke(remaining);
                return true;
            }
            return false;
        }

        public int GetUnlockedLevel()
        {
            return Mathf.Max(1, PlayerPrefs.GetInt(KEY_UNLOCKED_LEVEL, 1));
        }

        public void UnlockLevel(int level)
        {
            int current = GetUnlockedLevel();
            if (level > current)
            {
                PlayerPrefs.SetInt(KEY_UNLOCKED_LEVEL, level);
                PlayerPrefs.Save();
            }
        }

        public int GetSelectedKnife()
        {
            return PlayerPrefs.GetInt(KEY_SELECTED_KNIFE, 0);
        }

        public void SetSelectedKnife(int index)
        {
            PlayerPrefs.SetInt(KEY_SELECTED_KNIFE, index);
            PlayerPrefs.Save();
            OnKnifeSelected?.Invoke(index);
        }

        public bool IsKnifeOwned(int index)
        {
            if (index == 0) return true;
            return PlayerPrefs.GetInt(KEY_OWNED_KNIFE_PREFIX + index, 0) == 1;
        }

        public void SetKnifeOwned(int index, bool owned)
        {
            PlayerPrefs.SetInt(KEY_OWNED_KNIFE_PREFIX + index, owned ? 1 : 0);
            PlayerPrefs.Save();
        }

        public bool IsSoundEnabled()
        {
            return PlayerPrefs.GetInt(KEY_SOUND_ENABLED, 1) == 1;
        }

        public void SetSoundEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(KEY_SOUND_ENABLED, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public bool IsMusicEnabled()
        {
            return PlayerPrefs.GetInt(KEY_MUSIC_ENABLED, 1) == 1;
        }

        public void SetMusicEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(KEY_MUSIC_ENABLED, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public bool IsHapticsEnabled()
        {
            return PlayerPrefs.GetInt(KEY_HAPTICS_ENABLED, 1) == 1;
        }

        public void SetHapticsEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(KEY_HAPTICS_ENABLED, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        [ContextMenu("Reset All Save Data")]
        public void ResetSaveData()
        {
            PlayerPrefs.DeleteAll();
            InitializeDefaults();
            Debug.Log("[SaveManager] All player preferences and game progress have been reset.");
        }
    }
}
