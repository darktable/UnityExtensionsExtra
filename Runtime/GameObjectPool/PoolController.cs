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

        [SerializeField] bool _spawnBackupsOnAwake = false;
        [SerializeField] bool _destroyBackupsOnDestroy = false;
        [SerializeField] Settings[] _settings = default;

        void Awake()
        {
            if (_spawnBackupsOnAwake) SpawnBackups();
        }
        
        void OnDestroy()
        {
            if (_destroyBackupsOnDestroy) DestroyBackups();
        }

        public void SpawnBackups()
        {
            foreach (var s in _settings)
            {
                GameObjectPool.SpawnBackups(s.template, s.quantity);
            }
        }

        public void DestroyBackups()
        {
            foreach (var s in _settings)
            {
                GameObjectPool.DestroyBackups(s.template);
            }
        }

    } // class PoolController

} // namespace UnityExtensions