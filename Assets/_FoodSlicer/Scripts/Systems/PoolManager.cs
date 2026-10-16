using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodSlicer
{
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        private readonly Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<GameObject, string> instancePrefabKeys = new Dictionary<GameObject, string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            string key = prefab.name;
            if (!poolDictionary.ContainsKey(key))
            {
                poolDictionary[key] = new Queue<GameObject>();
            }

            GameObject instance;
            if (poolDictionary[key].Count > 0)
            {
                instance = poolDictionary[key].Dequeue();
                if (instance != null)
                {
                    instance.transform.SetPositionAndRotation(position, rotation);
                    if (parent != null) instance.transform.SetParent(parent);
                    instance.SetActive(true);
                    instancePrefabKeys[instance] = key;
                    return instance;
                }
            }

            instance = Instantiate(prefab, position, rotation, parent != null ? parent : transform);
            instance.name = prefab.name;
            instancePrefabKeys[instance] = key;
            return instance;
        }

        public void Despawn(GameObject instance, float delay = 0f)
        {
            if (instance == null) return;

            if (delay <= 0f || !Application.isPlaying)
            {
                Recycle(instance);
            }
            else
            {
                StartCoroutine(DespawnRoutine(instance, delay));
            }
        }

        private IEnumerator DespawnRoutine(GameObject instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            Recycle(instance);
        }

        private void Recycle(GameObject instance)
        {
            if (instance == null) return;

            if (instancePrefabKeys.TryGetValue(instance, out string key))
            {
                if (!poolDictionary.ContainsKey(key))
                {
                    poolDictionary[key] = new Queue<GameObject>();
                }

                // Reset physics if any
                Rigidbody rb = instance.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                instance.SetActive(false);
                instance.transform.SetParent(transform);
                poolDictionary[key].Enqueue(instance);
            }
            else
            {
                Destroy(instance);
            }
        }
    }
}
