using System.Collections.Generic;
using UnityEngine;

namespace FoodSlicer
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Knife Spawning")]
        [SerializeField] [Tooltip("Anchor transform where the active knife is mounted")]
        private Transform knifeSpawnPoint;

        [SerializeField] [Tooltip("List of all knife prefabs ordered by knife index")]
        private List<KnifeData> knifeList = new List<KnifeData>();

        private GameObject activeKnifeInstance;
        private KnifeController activeKnifeController;

        public KnifeController ActiveKnife => activeKnifeController;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (knifeList == null || knifeList.Count == 0)
            {
                var loaded = Resources.LoadAll<KnifeData>("Knives");
                if (loaded != null && loaded.Length > 0)
                {
                    knifeList = new List<KnifeData>(loaded);
                    knifeList.Sort((a, b) => a.knifeIndex.CompareTo(b.knifeIndex));
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
            SpawnActiveKnife();
        }

        public void SpawnActiveKnife()
        {
            if (KnifeInputController.Instance != null)
            {
                KnifeInputController.Instance.SetActiveKnife(null);
            }

            if (activeKnifeInstance != null)
            {
                Destroy(activeKnifeInstance);
                activeKnifeInstance = null;
                activeKnifeController = null;
            }
            else if (knifeSpawnPoint != null && knifeSpawnPoint.childCount > 0)
            {
                for (int i = knifeSpawnPoint.childCount - 1; i >= 0; i--)
                {
                    Destroy(knifeSpawnPoint.GetChild(i).gameObject);
                }
            }

            int selectedIndex = SaveManager.Instance != null ? SaveManager.Instance.GetSelectedKnife() : 0;
            selectedIndex = Mathf.Clamp(selectedIndex, 0, knifeList.Count - 1);

            GameObject prefabToSpawn = (knifeList != null && knifeList.Count > selectedIndex && knifeList[selectedIndex] != null)
                ? knifeList[selectedIndex].knifePrefab
                : null;

            if (prefabToSpawn != null && knifeSpawnPoint != null)
            {
                activeKnifeInstance = Instantiate(prefabToSpawn, knifeSpawnPoint.position, knifeSpawnPoint.rotation, knifeSpawnPoint);
                activeKnifeController = activeKnifeInstance.GetComponent<KnifeController>();
                if (KnifeInputController.Instance != null)
                {
                    KnifeInputController.Instance.SetActiveKnife(activeKnifeController);
                }
            }
        }
    }
}
