using System.Collections.Generic;
using UnityEngine;

namespace FoodSlicer
{
    public class SegmentedFood : MonoBehaviour
    {
        [Header("Food Configuration")]
        [SerializeField] private FoodData foodData;
        [SerializeField] private GameObject wholeModel;
        [SerializeField] private List<GameObject> visualSegments = new List<GameObject>();

        [Header("Slice Settings")]
        [SerializeField] private float cutSpacing = 0.35f;
        [SerializeField] private Vector3 cutDirection = Vector3.forward;

        [Header("Perfect Cut Tolerances")]
        [SerializeField] [Range(0.05f, 0.4f)] private float perfectCutTolerance = 0.18f;

        private int totalSegments = 1;
        private int currentCutIndex = 0;
        private bool isFullySliced = false;
        private FoodItem foodItem;

        public FoodData Data => foodData;
        public int TotalSegments => totalSegments;
        public int RemainingSegments => totalSegments - currentCutIndex;
        public bool IsFullySliced => isFullySliced;

        private void Awake()
        {
            foodItem = GetComponent<FoodItem>();
            InitializeSegments();
        }

        public void Setup(FoodData data, bool isTarget)
        {
            foodData = data;
            if (foodItem != null)
            {
                foodItem.SetTarget(isTarget);
            }
            InitializeSegments();
        }

        public void InitializeSegments()
        {
            currentCutIndex = 0;
            isFullySliced = false;

            if (visualSegments != null && visualSegments.Count > 0)
            {
                totalSegments = visualSegments.Count;
                for (int i = 0; i < visualSegments.Count; i++)
                {
                    if (visualSegments[i] != null)
                    {
                        visualSegments[i].SetActive(true);
                    }
                }
                if (wholeModel != null) wholeModel.SetActive(false);
            }
            else
            {
                totalSegments = foodData != null ? Mathf.Max(1, foodData.totalSegments) : 1;
                if (wholeModel != null) wholeModel.SetActive(true);
            }
        }

        public bool CutNextSegment(Vector3 bladeHitPoint, out bool isPerfect)
        {
            isPerfect = false;
            if (isFullySliced || currentCutIndex >= totalSegments) return false;

            Vector3 segmentSpawnPos = bladeHitPoint;
            Quaternion segmentSpawnRot = transform.rotation;

            // Determine if the cut was near the ideal segment cut position
            if (visualSegments != null && currentCutIndex < visualSegments.Count && visualSegments[currentCutIndex] != null)
            {
                GameObject currentSeg = visualSegments[currentCutIndex];
                segmentSpawnPos = currentSeg.transform.position;
                segmentSpawnRot = currentSeg.transform.rotation;

                float distToSegmentCenter = Mathf.Abs(bladeHitPoint.z - segmentSpawnPos.z);
                if (distToSegmentCenter <= perfectCutTolerance)
                {
                    isPerfect = true;
                }

                currentSeg.SetActive(false);
            }
            else
            {
                // Fallback for single/procedural foods
                isPerfect = Random.value < 0.25f; // occasional perfect cut
                if (wholeModel != null && currentCutIndex == 0)
                {
                    wholeModel.SetActive(false);
                }
            }

            // Spawn the detached physical slice piece
            GameObject slicePrefab = foodData != null ? foodData.slicePiecePrefab : null;
            if (slicePrefab != null)
            {
                GameObject piece = PoolManager.Instance != null
                    ? PoolManager.Instance.Spawn(slicePrefab, segmentSpawnPos, segmentSpawnRot)
                    : Instantiate(slicePrefab, segmentSpawnPos, segmentSpawnRot);

                FoodSlicePiece sliceComp = piece.GetComponent<FoodSlicePiece>();
                if (sliceComp != null)
                {
                    sliceComp.InitializePiece(Vector3.forward, 1f);
                }
            }

            currentCutIndex++;

            if (currentCutIndex >= totalSegments)
            {
                isFullySliced = true;
                if (foodItem != null)
                {
                    foodItem.OnFullySliced();
                }
            }

            return true;
        }

        public void AddVisualSegment(GameObject segmentObj)
        {
            if (segmentObj != null && !visualSegments.Contains(segmentObj))
            {
                visualSegments.Add(segmentObj);
                totalSegments = visualSegments.Count;
            }
        }
    }
}
