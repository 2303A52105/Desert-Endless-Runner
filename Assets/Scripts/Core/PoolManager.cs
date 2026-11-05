using System.Collections.Generic;
using UnityEngine;

namespace DesertRunner
{
    public class PooledObject : MonoBehaviour
    {
        [System.NonSerialized] public string Key;
        [System.NonSerialized] public bool InPool;
        public void Release() { PoolManager.Release(this); }
    }

    /// <summary>Releases a pooled object after Life seconds (only runs while active).</summary>
    public class AutoRelease : MonoBehaviour
    {
        public float Life = 1.2f;
        float timer;
        PooledObject pooled;

        void OnEnable()
        {
            timer = 0f;
            if (pooled == null) pooled = GetComponent<PooledObject>();
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= Life)
            {
                if (pooled != null) pooled.Release(); else gameObject.SetActive(false);
            }
        }
    }

    /// <summary>Generic keyed object pool. Factories are registered per scene.</summary>
    public static class PoolManager
    {
        static Transform root;
        static readonly Dictionary<string, System.Func<GameObject>> factories = new Dictionary<string, System.Func<GameObject>>();
        static readonly Dictionary<string, Stack<PooledObject>> pools = new Dictionary<string, Stack<PooledObject>>();

        public static void Init(Transform poolRoot)
        {
            root = poolRoot;
            factories.Clear();
            pools.Clear();
        }

        public static void Register(string key, System.Func<GameObject> factory)
        {
            factories[key] = factory;
            if (!pools.ContainsKey(key)) pools[key] = new Stack<PooledObject>();
        }

        static PooledObject Create(string key)
        {
            System.Func<GameObject> f;
            if (!factories.TryGetValue(key, out f)) { Debug.LogWarning("PoolManager: unknown key " + key); return null; }
            GameObject go = f();
            PooledObject po = go.GetComponent<PooledObject>();
            if (po == null) po = go.AddComponent<PooledObject>();
            po.Key = key;
            po.InPool = false;
            return po;
        }

        public static void Prewarm(string key, int count)
        {
            for (int i = 0; i < count; i++)
            {
                PooledObject po = Create(key);
                if (po != null) Release(po);
            }
        }

        public static GameObject Get(string key, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            Stack<PooledObject> stack;
            if (!pools.TryGetValue(key, out stack)) { stack = new Stack<PooledObject>(); pools[key] = stack; }
            PooledObject po = null;
            while (stack.Count > 0 && po == null) po = stack.Pop();
            if (po == null) po = Create(key);
            if (po == null) return null;
            po.InPool = false;
            Transform t = po.transform;
            t.SetParent(parent != null ? parent : root, false);
            t.SetPositionAndRotation(position, rotation);
            t.localScale = Vector3.one;
            po.gameObject.SetActive(true);
            return po.gameObject;
        }

        public static void Release(PooledObject po)
        {
            if (po == null || po.InPool) return;
            po.InPool = true;
            if (root != null) po.transform.SetParent(root, false);
            po.gameObject.SetActive(false);
            Stack<PooledObject> stack;
            if (!pools.TryGetValue(po.Key, out stack)) { stack = new Stack<PooledObject>(); pools[po.Key] = stack; }
            stack.Push(po);
        }
    }
}
