using System.Collections.Generic;
using UnityEngine;

namespace UnityExtensions
{
    using Pool = Stack<(GameObject gameObject, IRecyclable recyclable)>;
    struct InstanceInfo { public Pool pool; public IRecyclable recyclable; }

    public interface IRecyclable
    {
        void SetActive(bool active);
    }

    public struct GameObjectPool
    {
        static Transform _poolRoot;
        static Dictionary<GameObject, Pool> _templatePool = new Dictionary<GameObject, Pool>(32);
        static Dictionary<GameObject, InstanceInfo> _instanceInfo = new Dictionary<GameObject, InstanceInfo>(256);

        static Transform poolRoot
        {
            get
            {
                if (!_poolRoot)
                {
                    _poolRoot = new GameObject("Pool").transform;
                    _poolRoot.ResetLocal();
                    Object.DontDestroyOnLoad(_poolRoot.gameObject);
                }
                return _poolRoot;
            }
        }

        public static void Prepare(GameObject template, int quantity)
        {
            if (!_templatePool.TryGetValue(template, out var pool))
            {
                pool = new Pool(32);
                _templatePool.Add(template, pool);
            }

            while (quantity > 0)
            {
                var instance = Object.Instantiate(template, null, false);

                if (instance.TryGetComponent(out IRecyclable recyclable)) recyclable.SetActive(false);
                else instance.SetActive(false);

                instance.transform.SetParent(poolRoot, false);

                pool.Push((instance, recyclable));

                quantity--;
            }
        }

        public static void DestroyUnused(GameObject template)
        {
            if (_templatePool.TryGetValue(template, out var pool))
            {
                while (pool.Count > 0)
                {
                    Object.Destroy(pool.Pop().gameObject);
                }
            }
        }

        public static GameObject Spawn(GameObject template)
        {
            if (!_templatePool.TryGetValue(template, out var pool))
            {
                pool = new Pool(32);
                _templatePool.Add(template, pool);
            }

            GameObject instance;
            IRecyclable recyclable;

            if (pool.Count == 0)
            {
                instance = Object.Instantiate(template, null, false);
                instance.TryGetComponent(out recyclable);
            }
            else
            {
                (instance, recyclable) = pool.Pop();
                instance.transform.SetParent(null, false);
            }

            if (recyclable == null) instance.SetActive(true);
            else recyclable.SetActive(true);

            _instanceInfo.Add(instance, new InstanceInfo { pool = pool, recyclable = recyclable });

            return instance;
        }

        public static void Despawn(GameObject instance)
        {
            var info = _instanceInfo[instance];
            _instanceInfo.Remove(instance);

            if (info.recyclable == null) instance.SetActive(false);
            else info.recyclable.SetActive(false);

            instance.transform.SetParent(poolRoot, false);

            info.pool.Push((instance, info.recyclable));
        }

        public static void DespawnAll()
        {
            foreach (var p in _instanceInfo)
            {
                if (p.Value.recyclable == null) p.Key.SetActive(false);
                else p.Value.recyclable.SetActive(false);

                p.Key.transform.SetParent(poolRoot, false);

                p.Value.pool.Push((p.Key, p.Value.recyclable));
            }
            _instanceInfo.Clear();
        }

        public static void DestroyAll()
        {
            foreach (var instance in _instanceInfo.Keys)
            {
                Object.Destroy(instance);
            }
            _instanceInfo.Clear();

            foreach (var pool in _templatePool.Values)
            {
                while (pool.Count > 0)
                {
                    Object.Destroy(pool.Pop().gameObject);
                }
            }
            _templatePool.Clear();
        }

    } // GameObjectPool

} // namespace UnityExtensions