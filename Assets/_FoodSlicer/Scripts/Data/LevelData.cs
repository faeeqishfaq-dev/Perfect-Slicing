using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoodSlicer
{
    public enum TargetCountMode
    {
        CompletedFood,
        IndividualSlice
    }

    [Serializable]
    public class TargetFoodObjective
    {
        public FoodType foodType;
        [Tooltip("Number of this food item that must be fully cut to clear the objective")]
        public int requiredCount = 3;
        [Tooltip("Whether to count fully completed fruits or individual segment slices")]
        public TargetCountMode countMode = TargetCountMode.CompletedFood;
    }

    [CreateAssetMenu(fileName = "LevelData_", menuName = "FoodSlicer/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Level Information")]
        public int levelNumber = 1;
        public string levelName = "Fruit Valley";
        [Range(1, 5)] public int difficulty = 1;

        [Header("Conveyor Settings")]
        [Tooltip("Movement speed of incoming foods")]
        public float conveyorSpeed = 3.5f;

        [Tooltip("Gradual speed acceleration per cut item")]
        public float speedIncreasePerCut = 0.02f;

        [Tooltip("Maximum conveyor speed clamp")]
        public float maxConveyorSpeed = 6.0f;

        [Header("Spawn Settings")]
        [Tooltip("Base distance between successive spawned food items")]
        public float spawnSpacing = 3.0f;

        [Tooltip("Distance in front of player where foods spawn")]
        public float spawnDistanceZ = 22.0f;

        [Header("Food Objectives (Top 3 Target Slots)")]
        [Tooltip("Required target foods to complete this level (up to 3)")]
        public List<TargetFoodObjective> targetObjectives = new List<TargetFoodObjective>();

        [Header("Food Sequence")]
        [Tooltip("The ordered list of food data items that will spawn on the conveyor")]
        public List<FoodData> foodSequence = new List<FoodData>();

        [Header("Rules & Penalties")]
        public WrongFoodPenaltyMode wrongFoodPenaltyMode = WrongFoodPenaltyMode.None;
        public BoardHitMode boardHitMode = BoardHitMode.Penalty;
        [Tooltip("Score deducted on board hit if mode is Penalty")]
        public int boardPenaltyScore = 15;
        public MissPenaltyMode missPenaltyMode = MissPenaltyMode.ResetCombo;

        [Header("Rewards & Economy")]
        public int rewardCoins = 50;
        public bool hasUnlockReward = false;
        public string unlockRewardName = "";
        public Sprite unlockRewardIcon = null;

        [Header("Environment Theme")]
        public Color topBackgroundColor = new Color(0.35f, 0.75f, 1f); // Vibrant Sky/Cyan
        public Color bottomBackgroundColor = new Color(0.9f, 0.95f, 1f);
    }
}
