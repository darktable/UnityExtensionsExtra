using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityExtensions
{
    public abstract class BaseBlendingChannel<T>
    {
        public string name;
        public abstract T value { get; set; }
        public Blender<T> parent { get; internal set; }

        public BaseBlendingChannel() { }
        public BaseBlendingChannel(string name) => this.name = name;

        protected virtual string defaultName => "[Unnamed Channel]";
        internal string validName => string.IsNullOrEmpty(name) ? defaultName : name;
        public override string ToString() => $"{validName}: {value}";

#if UNITY_EDITOR

        public virtual void OnGUILayout()
        {
            EditorGUILayout.LabelField(validName, value.ToString());
        }

#endif
    }

    /// <summary>
    /// 混合通道
    /// </summary>
    public class BlendingChannel<T> : BaseBlendingChannel<T>
    {
        T _value;

        /// <summary>
        /// 通道值
        /// </summary>
        public override T value
        {
            get => _value;
            set
            {
                if (parent != null)
                {
                    if (!parent.Equals(_value, value))
                    {
                        _value = value;
                        parent.SetDirty();
                    }
                }
                else _value = value;
            }
        }

        public BlendingChannel(T value) => _value = value;
        public BlendingChannel(string name, T value) : base(name) => _value = value;
    }

} // namespace UnityExtensions