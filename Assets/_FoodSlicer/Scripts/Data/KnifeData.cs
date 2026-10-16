using UnityEngine;

namespace FoodSlicer
{
    [CreateAssetMenu(fileName = "KnifeData_", menuName = "FoodSlicer/Knife Data")]
    public class KnifeData : ScriptableObject
    {
        [Header("Knife Identity")]
        public string knifeName = "Chef Knife";
        public int knifeIndex = 0;
        public Sprite icon;
        public GameObject knifePrefab;

        [Header("Store & Economy")]
        public int price = 0;
        public bool unlockedByDefault = false;

        [Header("Visual & Blade Stats")]
        public Color bladeColor = Color.white;
        public Color handleColor = new Color(0.15f, 0.15f, 0.15f);
        [Range(0.5f, 2f)] public float chopSpeedMultiplier = 1f;
        public float bladeLength = 1.2f;
    }
}
