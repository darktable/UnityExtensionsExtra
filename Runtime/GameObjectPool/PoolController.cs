using UnityEngine;

namespace UnityExtensions
{
    [AddComponentMenu("Miscellaneous/Pool Controller")]
    [DisallowMultipleComponent]
    public class PoolController : ScriptableComponent
    {
        [System.Serializable]
        public struct Settings
        {
            public GameObject template;
            public int quantity;
        }

        [SerializeField] bool _prepareOnAwake = false;
        [SerializeField] bool _destroyUnusedOnDestroy = false;
        [SerializeField] Settings[] _settings = default;

        void Awake()
        {
            if (_prepareOnAwake) Prepare();
        }
        
        void OnDestroy()
        {
            if (_destroyUnusedOnDestroy) DestroyUnused();
        }

        public void Prepare()
        {
            foreach (var s in _settings)
            {
                GameObjectPool.Prepare(s.template, s.quantity);
            }
        }

        public void DestroyUnused()
        {
            foreach (var s in _settings)
            {
                GameObjectPool.DestroyUnused(s.template);
            }
        }

    } // class PoolController

} // namespace UnityExtensions