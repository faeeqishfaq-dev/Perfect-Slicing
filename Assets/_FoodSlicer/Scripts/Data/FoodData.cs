using UnityEngine;

namespace FoodSlicer
{
    public enum FoodCategory
    {
        Fruit,
        Vegetable,
        Bakery
    }

    [CreateAssetMenu(fileName = "FoodData_", menuName = "FoodSlicer/Food Data")]
    public class FoodData : ScriptableObject
    {
        [Header("Identity")]
        public FoodType foodType;
        public string foodName;
        public FoodCategory category;
        public Sprite icon;

        [Header("Prefabs & Mesh References")]
        [Tooltip("The whole uncut food prefab")]
        public GameObject uncutPrefab;
        [Tooltip("Prefab spawned when a segment is sliced off")]
        public GameObject slicePiecePrefab;

        [Header("Segmentation")]
        [Tooltip("Total number of slices/segments for this food item")]
        [Range(1, 10)] public int totalSegments = 4;
        [Tooltip("Distance along local Z/X between consecutive cut positions")]
        public float sliceSpacing = 0.35f;

        [Header("Scoring & Physics")]
        [Tooltip("Base score awarded per sliced segment")]
        public int scorePerSegment = 10;
        [Tooltip("Resistance/slowdown feel when blade impacts this food")]
        [Range(0f, 1f)] public float sliceResistance = 0.2f;

        [Header("Scale & Positioning")]
        [Tooltip("Adjustable visual scale for this specific food item")]
        public Vector3 visualScale = Vector3.one;

        [Tooltip("Vertical height offset relative to cutting board surface")]
        public float boardHeightOffset = 0f;

        [Header("Visual & VFX")]
        public Color exteriorColor = Color.white;
        public Color interiorColor = Color.white;
        public Color particleColor = Color.white;
        [Range(3, 30)] public int particleAmount = 12;
        public float particleScale = 0.15f;
    }
}
