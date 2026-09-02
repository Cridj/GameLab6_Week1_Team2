using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public enum PoolType
{
    Neutral,
    Medic,
    Police,
    Infected,
    Projectile
}

public class PoolManager : MonoBehaviour
{
    [System.Serializable]
    public class PoolConfig
    {
        public PoolType key;
        public GameObject prefab;
        public int initialSize;
    }

    public static PoolManager Instance { get; private set; }


    [SerializeField] private List<PoolConfig> poolConfigs;

    private Dictionary<PoolType, Queue<GameObject>> poolDictionary = new Dictionary<PoolType, Queue<GameObject>>();

    private Dictionary<PoolType, GameObject> prefabDictionary = new Dictionary<PoolType, GameObject>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        for (int i = 0; i < poolConfigs.Count; i++)
        {
            PoolConfig config = poolConfigs[i];

            Queue<GameObject> queue = new Queue<GameObject>();

            for (int j = 0; j < config.initialSize; j++)
            {
                GameObject obj = Instantiate(config.prefab, transform);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }

            poolDictionary.Add(config.key, queue);
            prefabDictionary.Add(config.key, config.prefab);
        }
    }

    public GameObject Get(PoolType key)
    {
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"해당 키의 풀이 없습니다: {key}");
            return null;
        }

        Queue<GameObject> queue = poolDictionary[key];

        GameObject obj;

        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
        }
        else
        {
            obj = Instantiate(prefabDictionary[key], transform);
        }

        obj.SetActive(true);
        return obj;
    }

    public void Return(PoolType key, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"해당 키의 풀이 없습니다: {key}");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        poolDictionary[key].Enqueue(obj);
    }

    public PoolType GetPoolType(GameObject obj)
    {
        return prefabDictionary.FirstOrDefault(x => x.Value == obj).Key;
    }
}
