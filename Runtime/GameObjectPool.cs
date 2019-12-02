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
            public int preallocateCount;
        }


        struct DelayDespawnObject
        {
            public float time;
            public GameObject gameObject;
        }


        [SerializeField]
        PoolSettings[] _poolSettings = default;


        static Dictionary<GameObject, Stack<GameObject>> _prefabToPool = new Dictionary<GameObject, Stack<GameObject>>();
        static Dictionary<GameObject, Stack<GameObject>> _objectToPool = new Dictionary<GameObject, Stack<GameObject>>();
        static QuickLinkedList<DelayDespawnObject> _delayDespawnObjects;
        static QuickLinkedList<DelayDespawnObject> _unscaledDelayDespawnObjects;


        void Awake()
        {
            for (int i = 0; i < _poolSettings.Length; i++)
            {
                ref var s = ref _poolSettings[i];
                Prepare(s.prefab, s.preallocateCount);
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
            
            if (pool.Count == 0) Prepare(prefab, 1);

            var obj = pool.Pop();
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


        public static void Despawn(GameObject gameObject, float delay, TimeMode timeMode = TimeMode.Normal)
        {
            if (delay > 0f)
            {
                if (_delayDespawnObjects.count == 0 && _unscaledDelayDespawnObjects.count == 0)
                {
                    RuntimeUtilities.waitForEndOfFrame += GlobalUpdate;
                }

                QuickLinkedList<DelayDespawnObject> list;
                float time;

                if (timeMode == TimeMode.Normal)
                {
                    if (_delayDespawnObjects == null) _delayDespawnObjects = new QuickLinkedList<DelayDespawnObject>();
                    list = _delayDespawnObjects;
                    time = delay + Time.time;
                }
                else
                {
                    if (_unscaledDelayDespawnObjects == null) _unscaledDelayDespawnObjects = new QuickLinkedList<DelayDespawnObject>();
                    list = _unscaledDelayDespawnObjects;
                    time = delay + Time.unscaledTime;
                }

                var newNodeValue = new DelayDespawnObject { time = time, gameObject = gameObject };

                int id = list.last;
                while (id >= 0)
                {
                    var node = list.GetNode(id);

                    if (node.value.time <= newNodeValue.time)
                    {
                        list.AddAfter(id, newNodeValue);
                        return;
                    }

                    id = node.previous;
                }
                list.AddFirst(newNodeValue);
            }
            else Despawn(gameObject);
        }


        static void GlobalUpdate()
        {
            bool CheckList(QuickLinkedList<DelayDespawnObject> list, float time)
            {
                if (list != null)
                {
                    while (list.first >= 0)
                    {
                        var item = list[list.first];

                        if (item.time > time)
                        {
                            return false;
                        }

                        Despawn(item.gameObject);

                        list.RemoveFirst();
                    }
                }
                return true;
            }

            bool result1 = CheckList(_delayDespawnObjects, Time.time);
            bool result2 = CheckList(_unscaledDelayDespawnObjects, Time.unscaledTime);

            if (result1 && result2)
            {
                RuntimeUtilities.waitForEndOfFrame -= GlobalUpdate;
            }
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
            if (_delayDespawnObjects != null) _delayDespawnObjects.Clear();
            if (_unscaledDelayDespawnObjects != null) _unscaledDelayDespawnObjects.Clear();
        }

    } // class GameObjectPool

} // namespace UnityExtensions