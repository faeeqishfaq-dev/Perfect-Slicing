using UnityEngine;

namespace FoodSlicer
{
    public class FoodMover : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float currentSpeed = 4.0f;
        [SerializeField] private float despawnZ = -4.0f;

        private FoodItem foodItem;
        private bool isMoving = true;

        public float CurrentSpeed { get => currentSpeed; set => currentSpeed = value; }

        private void Awake()
        {
            foodItem = GetComponent<FoodItem>();
        }

        private void Update()
        {
            if (!isMoving) return;
            if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying()) return;

            transform.position += Vector3.back * (currentSpeed * Time.deltaTime);

            if (transform.position.z <= despawnZ)
            {
                OnReachedDespawn();
            }
        }

        private void OnReachedDespawn()
        {
            isMoving = false;

            if (foodItem != null && !foodItem.IsCompleted)
            {
                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.RegisterFoodMissed(foodItem);
                }
            }

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ResumeMoving()
        {
            isMoving = true;
        }

        public void StopMoving()
        {
            isMoving = false;
        }
    }
}
