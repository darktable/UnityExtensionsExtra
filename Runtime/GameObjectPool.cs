using System.Collections.Generic;
using UnityEngine;

namespace UnityExtensions
{
    [AddComponentMenu("Miscellaneous/Game Object Pool")]
    [DisallowMultipleComponent]
    public class GameObjectPool : ScriptableComponent
    {
        [System.Serializable]
        struct PoolSettings
        {
            public GameObject prefab;
            public int initialQuantity;
        }

        [SerializeField]
        PoolSettings[] _poolSettings = default;

        static Dictionary<GameObject, Stack<GameObject>> _prefabToPool = new Dictionary<GameObject, Stack<GameObject>>();
        static Dictionary<GameObject, Stack<GameObject>> _objectToPool = new Dictionary<GameObject, Stack<GameObject>>();

        void Awake()
        {
            for (int i = 0; i < _poolSettings.Length; i++)
            {
                ref var s = ref _poolSettings[i];
                Prepare(s.prefab, s.initialQuantity);
            }
        }

        public static void Prepare(GameObject prefab, int quantity)
        {
            if (!_prefabToPool.TryGetValue(prefab, out var pool))
            {
                pool = new Stack<GameObject>();
                _prefabToPool.Add(prefab, pool);
            }

            while (quantity > 0)
            {
                var obj = Instantiate(prefab);
                obj.SetActive(false);
                obj.transform.SetParent(RuntimeUtilities.globalTransform, false);

                pool.Push(obj);

                quantity--;
            }
        }

        public static void DestroyUnused(GameObject prefab)
        {
            if (prefab && _prefabToPool.TryGetValue(prefab, out var pool))
            {
                while (pool.Count > 0)
                {
                    Destroy(pool.Pop());
                }
            }
        }

        public static GameObject Spawn(GameObject prefab)
        {
            if (!_prefabToPool.TryGetValue(prefab, out var pool))
            {
                pool = new Stack<GameObject>();
                _prefabToPool.Add(prefab, pool);
            }
            
            var obj = pool.Count == 0 ? Instantiate(prefab) : pool.Pop();
            _objectToPool.Add(obj, pool);

            obj.transform.SetParent(null, false);
            obj.SetActive(true);

            return obj;
        }

        public static bool Despawn(GameObject gameObject)
        {
            if (gameObject && _objectToPool.TryGetValue(gameObject, out var pool))
            {
                gameObject.SetActive(false);
                gameObject.transform.SetParent(RuntimeUtilities.globalTransform, false);

                pool.Push(gameObject);
                _objectToPool.Remove(gameObject);

                return true;
            }
            return false;
        }

        public static void DespawnAll()
        {
            foreach (var p in _objectToPool)
            {
                if (p.Key)
                {
                    p.Key.SetActive(false);
                    p.Key.transform.SetParent(RuntimeUtilities.globalTransform, false);

                    p.Value.Push(p.Key);
                }
            }
            _objectToPool.Clear();
        }

    } // class GameObjectPool

} // namespace UnityExtensions