using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityExtensions
{
    using Pool = Stack<(GameObject gameObject, IDeactivatable deactivatable)>;
    struct InstanceInfo { public Pool pool; public IDeactivatable deactivatable; }

    public struct GameObjectPool
    {
        static Transform _poolRoot;
        static Dictionary<GameObject, Pool> _templateToPool = new Dictionary<GameObject, Pool>(32);
        static Dictionary<GameObject, InstanceInfo> _instanceToInfo = new Dictionary<GameObject, InstanceInfo>(256);

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

        /// <summary>
        /// SetActive(false) or Deactivate() is called.
        /// </summary>
        public static void Prepare(GameObject template, int quantity)
        {
            if (!_templateToPool.TryGetValue(template, out var pool))
            {
                pool = new Pool(32);
                _templateToPool.Add(template, pool);
            }

            while (quantity > 0)
            {
                var instance = Object.Instantiate(template, null, false);

                if (instance.TryGetComponent(out IDeactivatable deactivatable)) deactivatable.Deactivate();
                else instance.SetActive(false);

                instance.transform.SetParent(poolRoot, false);

                pool.Push((instance, deactivatable));

                quantity--;
            }
        }

        public static void DestroyUnused(GameObject template)
        {
            if (_templateToPool.TryGetValue(template, out var pool))
            {
                while (pool.Count > 0)
                {
                    Object.Destroy(pool.Pop().gameObject);
                }
            }
        }

        static void Spawn<TRecyclable>(GameObject template, out GameObject instance, out TRecyclable recyclable) where TRecyclable : IDeactivatable
        {
            if (!_templateToPool.TryGetValue(template, out var pool))
            {
                pool = new Pool(32);
                _templateToPool.Add(template, pool);
            }

            if (pool.Count == 0)
            {
                instance = Object.Instantiate(template, null, false);
                SceneManager.MoveGameObjectToScene(instance, poolRoot.gameObject.scene);
                instance.TryGetComponent(out recyclable);
            }
            else
            {
                var (gameObject, deactivatable) = pool.Pop();
                instance = gameObject;
                recyclable = (TRecyclable)deactivatable;
                instance.transform.SetParent(null, false);
            }

            _instanceToInfo.Add(instance, new InstanceInfo { pool = pool, deactivatable = recyclable });
        }

        /// <summary>
        /// SetActive(true) or Activate() is called.
        /// </summary>
        public static GameObject Spawn(GameObject template)
        {
            Spawn<IRecyclable>(template, out var instance, out var recyclable);
            if (recyclable == null) instance.SetActive(true);
            else recyclable.Activate();
            return instance;
        }

        /// <summary>
        /// Activate(T) is called.
        /// </summary>
        public static GameObject Spawn<T>(GameObject template, T a)
        {
            Spawn<IRecyclable<T>>(template, out var instance, out var recyclable);
            recyclable.Activate(a);
            return instance;
        }

        /// <summary>
        /// Activate(T1, T2) is called.
        /// </summary>
        public static GameObject Spawn<T1, T2>(GameObject template, T1 a, T2 b)
        {
            Spawn<IRecyclable<T1, T2>>(template, out var instance, out var recyclable);
            recyclable.Activate(a, b);
            return instance;
        }

        /// <summary>
        /// Activate(T1, T2, T3) is called.
        /// </summary>
        public static GameObject Spawn<T1, T2, T3>(GameObject template, T1 a, T2 b, T3 c)
        {
            Spawn<IRecyclable<T1, T2, T3>>(template, out var instance, out var recyclable);
            recyclable.Activate(a, b, c);
            return instance;
        }

        /// <summary>
        /// Activate(T1, T2, T3, T4) is called.
        /// </summary>
        public static GameObject Spawn<T1, T2, T3, T4>(GameObject template, T1 a, T2 b, T3 c, T4 d)
        {
            Spawn<IRecyclable<T1, T2, T3, T4>>(template, out var instance, out var recyclable);
            recyclable.Activate(a, b, c, d);
            return instance;
        }

        /// <summary>
        /// SetActive(false) or Deactivate() is called.
        /// </summary>
        public static void Despawn(GameObject instance)
        {
            var info = _instanceToInfo[instance];
            _instanceToInfo.Remove(instance);

            if (info.deactivatable == null) instance.SetActive(false);
            else info.deactivatable.Deactivate();

            instance.transform.SetParent(poolRoot, false);

            info.pool.Push((instance, info.deactivatable));
        }

        /// <summary>
        /// SetActive(false) or Deactivate() is called.
        /// </summary>
        public static void DespawnAll()
        {
            foreach (var p in _instanceToInfo)
            {
                if (p.Value.deactivatable == null) p.Key.SetActive(false);
                else p.Value.deactivatable.Deactivate();

                p.Key.transform.SetParent(poolRoot, false);

                p.Value.pool.Push((p.Key, p.Value.deactivatable));
            }
            _instanceToInfo.Clear();
        }

        /// <summary>
        /// For instances in use, SetActive(false) or Deactivate() is called.
        /// </summary>
        public static void DestroyAll()
        {
            foreach (var p in _instanceToInfo)
            {
                if (p.Value.deactivatable == null) p.Key.SetActive(false);
                else p.Value.deactivatable.Deactivate();
                Object.Destroy(p.Key);
            }
            _instanceToInfo.Clear();

            foreach (var pool in _templateToPool.Values)
            {
                while (pool.Count > 0)
                {
                    Object.Destroy(pool.Pop().gameObject);
                }
            }
            _templateToPool.Clear();
        }

    } // GameObjectPool

} // namespace UnityExtensions