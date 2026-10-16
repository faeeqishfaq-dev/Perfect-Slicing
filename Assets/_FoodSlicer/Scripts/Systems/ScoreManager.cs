using System;
using UnityEngine;

namespace FoodSlicer
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Score Values")]
        [SerializeField] [Tooltip("Base points awarded per sliced segment")]
        private int baseScorePerSegment = 10;

        [SerializeField] [Tooltip("Bonus points awarded for a perfect cut")]
        private int perfectCutBonus = 25;

        private int currentScore = 0;

        public int CurrentScore => currentScore;

        public event Action<int, int, bool> OnScoreAdded; // (addedPoints, totalScore, isPerfect)
        public event Action<int> OnScoreChanged;

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

        public void ResetScore()
        {
            currentScore = 0;
            OnScoreChanged?.Invoke(currentScore);
        }

        public int AddSliceScore(int customBaseScore = 0, bool isPerfect = false, int comboMultiplier = 1)
        {
            int basePoints = customBaseScore > 0 ? customBaseScore : baseScorePerSegment;
            int totalSegmentPoints = basePoints * Mathf.Max(1, comboMultiplier);

            if (isPerfect)
            {
                totalSegmentPoints += perfectCutBonus * Mathf.Max(1, comboMultiplier);
            }

            currentScore += totalSegmentPoints;
            OnScoreAdded?.Invoke(totalSegmentPoints, currentScore, isPerfect);
            OnScoreChanged?.Invoke(currentScore);

            return totalSegmentPoints;
        }

        public void DeductScore(int points)
        {
            if (points <= 0) return;
            currentScore = Mathf.Max(0, currentScore - points);
            OnScoreChanged?.Invoke(currentScore);
        }
    }
}
