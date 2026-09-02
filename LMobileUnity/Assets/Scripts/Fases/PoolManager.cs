using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [System.Serializable]
    public class PoolConfig
    {
        public GameObject prefab;
        public int initialSize = 5;
    }

    [Header("Pool Configurations")]
    [SerializeField] private List<PoolConfig> poolsToPreload;

    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<GameObject, GameObject> activeObjectToPrefab = new Dictionary<GameObject, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializePools();
    }

    private void InitializePools()
    {
        if (poolsToPreload == null) return;

        foreach (var config in poolsToPreload)
        {
            if (config.prefab == null) continue;

            if (!poolDictionary.ContainsKey(config.prefab))
            {
                poolDictionary[config.prefab] = new Queue<GameObject>();
                for (int i = 0; i < config.initialSize; i++)
                {
                    GameObject obj = Instantiate(config.prefab, transform);
                    obj.SetActive(false);
                    poolDictionary[config.prefab].Enqueue(obj);
                }
            }
        }
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;

        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary[prefab] = new Queue<GameObject>();
        }

        GameObject obj = null;
        Queue<GameObject> queue = poolDictionary[prefab];

        while (queue.Count > 0)
        {
            obj = queue.Dequeue();
            if (obj != null)
            {
                break;
            }
        }

        if (obj == null)
        {
            obj = Instantiate(prefab, transform);
        }

        obj.transform.SetParent(parent != null ? parent : transform);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        activeObjectToPrefab[obj] = prefab;
        return obj;
    }

    public void Release(GameObject obj)
    {
        if (obj == null) return;

        if (activeObjectToPrefab.TryGetValue(obj, out GameObject prefab))
        {
            activeObjectToPrefab.Remove(obj);
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            
            if (poolDictionary.ContainsKey(prefab))
            {
                poolDictionary[prefab].Enqueue(obj);
            }
        }
        else
        {
            // Fallback for non-pooled or unmanaged objects
            obj.SetActive(false);
        }
    }
}