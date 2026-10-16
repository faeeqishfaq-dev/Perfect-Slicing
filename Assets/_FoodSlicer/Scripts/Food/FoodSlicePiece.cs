using UnityEngine;

namespace FoodSlicer
{
    public class FoodSlicePiece : MonoBehaviour
    {
        [Header("Conveyor Motion")]
        [SerializeField] [Tooltip("Speed at which sliced piece travels down the board")]
        private float conveyorSpeed = 3.8f;

        [SerializeField] [Tooltip("Initial separation push along the board")]
        private float separationOffset = 0.16f;

        [SerializeField] [Range(1f, 8f)] [Tooltip("How long cut pieces remain on board before recycling")]
        private float pieceLifetime = 4.0f;

        private Rigidbody rb;
        private bool isMoving = true;
        private float spawnTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        public void InitializePiece(Vector3 bladeDirection, float slicePower = 1f)
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if (FoodSpawner.Instance != null)
            {
                conveyorSpeed = FoodSpawner.Instance.CurrentConveyorSpeed;
            }

            // Clean backward separation: gap between cut pieces
            transform.position += Vector3.back * separationOffset;

            // Rest flat on the cutting board surface
            Vector3 pos = transform.position;
            pos.y = 0.05f;
            transform.position = pos;

            isMoving = true;
            spawnTime = Time.time;
        }

        private void Update()
        {
            if (!isMoving) return;
            if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying()) return;

            transform.position += Vector3.back * (conveyorSpeed * Time.deltaTime);

            if (Time.time >= spawnTime + pieceLifetime || transform.position.z <= -4.5f)
            {
                RecyclePiece();
            }
        }

        private void RecyclePiece()
        {
            isMoving = false;
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}

