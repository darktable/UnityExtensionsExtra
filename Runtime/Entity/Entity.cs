using UnityEngine;

namespace UnityExtensions
{
    public partial class Entity<T> : HierarchialComponent<T> where T : Entity<T>
    {
        [SerializeField, HideInInspector]
        bool _localActive;

        //        public abstract string name { get; set; }

        //        public bool active
        //        {
        //            get
        //            {
        //                var node = this;
        //                while (node._localActive)
        //                {
        //                    node = node.parent;
        //                    if (node == null) return true;
        //                }
        //                return false;
        //            }
        //        }

        //        public bool localActive
        //        {
        //            get => _localActive;
        //            set
        //            {
        //                if (_localActive != value)
        //                {
        //                    _localActive = value;

        //                    if (parent == null || parent.globalActive)
        //                    {
        //                        if (value)
        //                        {
        //                            OnActivate();
        //                        }
        //                        else OnDeactivate();
        //                    }
        //                }
        //            }
        //        }

        //        protected virtual void OnActivate()
        //        {
        //            gameObject.SetActive(true);
        //        }

        //        protected virtual void OnDeactivate()
        //        {
        //            gameObject.SetActive(false);
        //        }



        //        public bool IsChildOf(Entity entity)
        //        {
        //            if (!entity)
        //            {
        //                throw new System.ArgumentNullException(nameof(entity));
        //            }

        //            var node = this;
        //            do
        //            {
        //                if (node == entity) return true;
        //                node = node._parent;
        //            }
        //            while (node);

        //            return false;
        //        }

        //        protected virtual void OnLoseChild(Entity target) { }
        //        protected virtual void OnGainChild(Entity target) { }
        //        protected virtual void ParentChanged(Entity originalParent, Entity currentParent) { }

        //        void Awake()
        //        {
        //            Debug.Log("Awake");
        //        }

        //        void OnDestroy()
        //        {
        //            Debug.Log("OnDestroy");
        //        }

        //#if UNITY_EDITOR

        //        bool _initialized = false;
        //        Entity _trackedParent;

        //        protected virtual void Reset()
        //        {
        //            if (transform.parent)
        //            {
        //                parent = transform.parent.GetComponentInParent<Entity>();
        //            }
        //        }

        //        protected virtual void OnValidate()
        //        {
        //            if (_parent != _trackedParent)
        //            {
        //                RuntimeUtilities.Swap(ref _parent, ref _trackedParent);
        //                parent = _trackedParent;
        //            }
        //        }

        //        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        //        void ISerializationCallbackReceiver.OnAfterDeserialize()
        //        {
        //            if (!_initialized)
        //            {
        //                _trackedParent = _parent;
        //                _initialized = true;
        //            }
        //        }

        //#endif

    } // class Entity<T>
}
