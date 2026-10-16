using UnityEngine;

namespace FoodSlicer
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Default Particle Prefabs")]
        [SerializeField] [Tooltip("Juice droplet / crumb particle system prefab")]
        private GameObject sliceParticlePrefab;

        [SerializeField] [Tooltip("Sparks / dust effect on board hit")]
        private GameObject boardHitParticlePrefab;

        [SerializeField] [Tooltip("Confetti burst on level win")]
        private GameObject levelWinParticlePrefab;

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

        public void PlaySliceEffect(Vector3 position, FoodData foodData)
        {
            if (sliceParticlePrefab == null || foodData == null) return;

            GameObject obj = PoolManager.Instance != null
                ? PoolManager.Instance.Spawn(sliceParticlePrefab, position, Quaternion.identity)
                : Instantiate(sliceParticlePrefab, position, Quaternion.identity);

            ParticleSystem ps = obj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = foodData.particleColor;
                main.startSize = foodData.particleScale;

                var emission = ps.emission;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, foodData.particleAmount) });

                ps.Clear();
                ps.Play();
            }

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(obj, 1.2f);
            }
        }

        public void PlayBoardHitEffect(Vector3 position)
        {
            if (boardHitParticlePrefab == null) return;

            GameObject obj = PoolManager.Instance != null
                ? PoolManager.Instance.Spawn(boardHitParticlePrefab, position, Quaternion.identity)
                : Instantiate(boardHitParticlePrefab, position, Quaternion.identity);

            ParticleSystem ps = obj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Clear();
                ps.Play();
            }

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(obj, 1.0f);
            }
        }

        public void PlayLevelWinEffect(Vector3 position)
        {
            if (levelWinParticlePrefab == null) return;

            GameObject obj = PoolManager.Instance != null
                ? PoolManager.Instance.Spawn(levelWinParticlePrefab, position, Quaternion.identity)
                : Instantiate(levelWinParticlePrefab, position, Quaternion.identity);

            ParticleSystem ps = obj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Clear();
                ps.Play();
            }

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(obj, 3.0f);
            }
        }
    }
}
