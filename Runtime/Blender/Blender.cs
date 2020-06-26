using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityExtensions
{
    /// <summary>
    /// 混合器，用于控制多输入单输出。
    /// </summary>
    public abstract class Blender<T> : BaseBlendingChannel<T>
    {
        List<BaseBlendingChannel<T>> _channels;
        T _baseValue;
        bool _isDirty;
        T _value;

        /// <summary>
        /// 任意通道的新增、删除、修改都会触发
        /// </summary>
        public event Action onValueChange;

        /// <summary>
        /// 通道总数
        /// </summary>
        public int channelCount => _channels == null ? 0 : _channels.Count;

        public T baseValue
        {
            get => _baseValue;
            set
            {
                if (!Equals(_baseValue, value))
                {
                    _baseValue = value;
                    SetDirty();
                }
            }
        }

        /// <summary>
        /// 所有通道混合后的输出值
        /// </summary>
        public override T value
        {
            get
            {
                if (_isDirty)
                {
                    _value = baseValue;
                    if (!RuntimeUtilities.IsNullOrEmpty(_channels))
                    {
                        for (int i = 0; i < _channels.Count; i++)
                            _value = Blend(_value, _channels[i].value);
                    }
                    _isDirty = false;
                }
                return _value;
            }
            set => throw new NotSupportedException();
        }

        public Blender(T baseValue)
        {
            _baseValue = baseValue;
            _isDirty = false;
            _value = baseValue;
        }

        public Blender(string name, T baseValue) : this(baseValue) => this.name = name;

        public void AddChannel(BaseBlendingChannel<T> channel)
        {
            if (channel.parent != null)
            {
                throw new Exception("Parent of channel is not null.");
            }

            if (_channels == null) _channels = new List<BaseBlendingChannel<T>>(4);

            channel.parent = this;
            _channels.Add(channel);
            SetDirty();
        }

        public void RemoveChannel(BaseBlendingChannel<T> channel)
        {
            if (channel.parent != this)
            {
                throw new Exception("Parent of channel is not this.");
            }

            channel.parent = null;
            _channels.Remove(channel);
            SetDirty();
        }

        internal void SetDirty()
        {
            _isDirty = true;
            OnValueChange();
        }

        protected void OnValueChange()
        {
            onValueChange?.Invoke();
            parent?.SetDirty();
        }

        /// <summary>
        /// 判断两个值是否相等
        /// </summary>
        public abstract bool Equals(T a, T b);

        /// <summary>
        /// 混合值
        /// </summary>
        public abstract T Blend(T a, T b);

        protected override string defaultName => "[Unnamed Blender]";

#if UNITY_EDITOR

        protected bool foldout { get; private set; }

        public override void OnGUILayout()
        {
            var rect = EditorGUILayout.GetControlRect();
            foldout = EditorGUI.Foldout(rect, foldout, GUIContent.none);
            EditorGUI.LabelField(rect, validName, value.ToString());

            if (foldout)
            {
                using (Editor.IndentLevelScope.New())
                {
                    EditorGUILayout.LabelField("Base Value", baseValue.ToString());
                    if (_channels != null)
                    {
                        foreach (var c in _channels)
                        {
                            c.OnGUILayout();
                        }
                    }
                }
            }
        }

#endif

    } // class Blender<T>


    /// <summary>
    /// 事件混合器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public abstract class EventBlender<T> : Blender<T>
    {
        // 混合事件
        private struct Event
        {
            public float time01;
            public T value;
            public float startDelay;
            public float duration;
            public AnimationCurve attenuation;

            public T GetScaledValue(EventBlender<T> blender)
            {
                return blender.Scale(value, Mathf.Max(attenuation.Evaluate(time01), 0f));
            }
        }

        List<Event> _events;
        bool _hasEventsValue;
        T _eventsValue;

        public override T value
        {
            get => _hasEventsValue ? Blend(base.value, _eventsValue) : base.value;
        }

        public EventBlender(T baseValue) : base(baseValue) { }
        public EventBlender(string name, T baseValue) : base(name, baseValue) { }

        /// <summary>
        /// 创建事件
        /// 事件在超过作用时间后自动移除
        /// </summary>
        /// <param name="attenuation"> 曲线的 Y 值被截断为非负数 </param>
        public void AddEvent(float startDelay, float duration, T value, AnimationCurve attenuation)
        {
            if (_events == null) _events = new List<Event>(4);
            _events.Add(new Event
            {
                time01 = 0,
                value = value,
                startDelay = startDelay,
                duration = duration,
                attenuation = attenuation,
            });
        }

        /// <summary>
        /// 如果使用了事件，需要以一定频率调用此方法
        /// </summary>
        public void UpdateEvents(float deltaTime)
        {
            bool lastHasEventsValue = _hasEventsValue;
            _hasEventsValue = false;

            if (!RuntimeUtilities.IsNullOrEmpty(_events))
            {
                Event e;
                float dt;

                for (int i = 0; i < _events.Count; i++)
                {
                    e = _events[i];
                    dt = deltaTime;

                    if (e.startDelay > 0f)
                    {
                        e.startDelay -= deltaTime;
                        if (e.startDelay <= 0f)
                        {
                            dt = -e.startDelay;
                            e.startDelay = 0f;
                        }
                        _events[i] = e;
                    }

                    if (e.startDelay == 0f)
                    {
                        e.time01 += dt / e.duration;

                        if (e.time01 >= 1f)
                        {
                            _events.RemoveAt(i--);
                            e.time01 = 1f;
                        }
                        else _events[i] = e;

                        if (_hasEventsValue)
                        {
                            _eventsValue = Blend(_eventsValue, e.GetScaledValue(this));
                        }
                        else
                        {
                            _hasEventsValue = true;
                            _eventsValue = e.GetScaledValue(this);
                        }
                    }
                }
            }

            if (lastHasEventsValue || _hasEventsValue)
            {
                OnValueChange();
            }
        }

        /// <summary>
        /// 清除所有事件
        /// </summary>
        public void ClearEvents()
        {
            if (_hasEventsValue)
            {
                OnValueChange();
                _hasEventsValue = false;
            }
            if (_events != null) _events.Clear();
        }

        /// <summary>
        /// 缩放值
        /// </summary>
        public abstract T Scale(T a, float b);

#if UNITY_EDITOR

        public override void OnGUILayout()
        {
            base.OnGUILayout();

            if (foldout && _hasEventsValue)
            {
                using (Editor.IndentLevelScope.New())
                {
                    EditorGUILayout.LabelField("Events Value", _eventsValue.ToString());
                }
            }
        }

#endif

    } // class EventBlender<T>


    /// <summary>
    /// Bool 混合器，用于控制多输入单输出。
    /// </summary>
    public abstract class BoolBlender : Blender<bool>
    {
        public BoolBlender(bool baseValue) : base(baseValue) { }
        public BoolBlender(string name, bool baseValue) : base(name, baseValue) { }

        public sealed override bool Equals(bool a, bool b)
        {
            return a == b;
        }

    } // class BoolBlender


    /// <summary>
    /// Float 混合器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public abstract class FloatBlender : EventBlender<float>
    {
        public FloatBlender(float baseValue) : base(baseValue) { }
        public FloatBlender(string name, float baseValue) : base(name, baseValue) { }

        public sealed override float Scale(float a, float b)
        {
            return a * b;
        }

        public sealed override bool Equals(float a, float b)
        {
            return a == b;
        }

        /// <summary>
        /// 创建事件
        /// 事件在超过作用时间后自动移除
        /// </summary>
        public void AddEvent(FloatBlendingEventPreset preset)
        {
            AddEvent(preset.startDelay, preset.duration, preset.value, preset.attenuation);
        }

    } // class FloatBlender


    /// <summary>
    /// Vector2 混合器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public abstract class Vector2Blender : EventBlender<Vector2>
    {
        public Vector2Blender(Vector2 baseValue) : base(baseValue) { }
        public Vector2Blender(string name, Vector2 baseValue) : base(name, baseValue) { }

        public sealed override Vector2 Scale(Vector2 a, float b)
        {
            return a * b;
        }

        public sealed override bool Equals(Vector2 a, Vector2 b)
        {
            return a.x == b.x && a.y == b.y;
        }

        /// <summary>
        /// 创建事件
        /// 事件在超过作用时间后自动移除
        /// </summary>
        public void AddEvent(Vector2BlendingEventPreset preset)
        {
            AddEvent(preset.startDelay, preset.duration, preset.value, preset.attenuation);
        }

    } // class Vector2Blender


    /// <summary>
    /// 与混合器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class BoolAndBlender : BoolBlender
    {
        public BoolAndBlender(bool baseValue = true) : base(baseValue) { }
        public BoolAndBlender(string name, bool baseValue = true) : base(name, baseValue) { }

        public sealed override bool Blend(bool a, bool b)
        {
            return a && b;
        }
    }


    /// <summary>
    /// 或混合器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class BoolOrBlender : BoolBlender
    {
        public BoolOrBlender(bool baseValue = false) : base(baseValue) { }
        public BoolOrBlender(string name, bool baseValue = false) : base(name, baseValue) { }

        public sealed override bool Blend(bool a, bool b)
        {
            return a || b;
        }
    }


    /// <summary>
    /// 加法混合器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class FloatAdditiveBlender : FloatBlender
    {
        public FloatAdditiveBlender(float baseValue = 0f) : base(baseValue) { }
        public FloatAdditiveBlender(string name, float baseValue = 0f) : base(name, baseValue) { }

        public float averageChannelValue => (value - baseValue) / channelCount;

        public sealed override float Blend(float a, float b)
        {
            return a + b;
        }
    }


    /// <summary>
    /// 乘法混合器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class FloatMultiplyBlender : FloatBlender
    {
        public FloatMultiplyBlender(float baseValue = 1f) : base(baseValue) { }
        public FloatMultiplyBlender(string name, float baseValue = 1f) : base(name, baseValue) { }

        public sealed override float Blend(float a, float b)
        {
            return a * b;
        }
    }


    /// <summary>
    /// 最大值混合器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class FloatMaximumBlender : FloatBlender
    {
        public FloatMaximumBlender(float baseValue) : base(baseValue) { }
        public FloatMaximumBlender(string name, float baseValue) : base(name, baseValue) { }

        public sealed override float Blend(float a, float b)
        {
            return Mathf.Max(a, b);
        }
    }


    /// <summary>
    /// 最小值混合器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class FloatMinimumBlender : FloatBlender
    {
        public FloatMinimumBlender(float baseValue) : base(baseValue) { }
        public FloatMinimumBlender(string name, float baseValue) : base(name, baseValue) { }

        public sealed override float Blend(float a, float b)
        {
            return Mathf.Min(a, b);
        }
    }


    /// <summary>
    /// 加法混合器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class Vector2AdditiveBlender : Vector2Blender
    {
        public Vector2AdditiveBlender(Vector2 baseValue = default) : base(baseValue) { }
        public Vector2AdditiveBlender(string name, Vector2 baseValue = default) : base(name, baseValue) { }

        public Vector2 averageChannelValue => (value - baseValue) / channelCount;

        public sealed override Vector2 Blend(Vector2 a, Vector2 b)
        {
            return a + b;
        }
    }


    /// <summary>
    /// 乘法混合器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class Vector2MultiplyBlender : Vector2Blender
    {
        public Vector2MultiplyBlender(Vector2 baseValue) : base(baseValue) { }
        public Vector2MultiplyBlender(string name, Vector2 baseValue) : base(name, baseValue) { }
        public Vector2MultiplyBlender() : base(new Vector2(1f, 1f)) { }
        public Vector2MultiplyBlender(string name) : base(name, new Vector2(1f, 1f)) { }

        public sealed override Vector2 Blend(Vector2 a, Vector2 b)
        {
            return Vector2.Scale(a, b);
        }
    }


    /// <summary>
    /// 最大值混合器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class Vector2MaximumBlender : Vector2Blender
    {
        public Vector2MaximumBlender(Vector2 baseValue) : base(baseValue) { }
        public Vector2MaximumBlender(string name, Vector2 baseValue) : base(name, baseValue) { }

        public sealed override Vector2 Blend(Vector2 a, Vector2 b)
        {
            return Vector2.Max(a, b);
        }
    }


    /// <summary>
    /// 最小值混合器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class Vector2MinimumBlender : Vector2Blender
    {
        public Vector2MinimumBlender(Vector2 baseValue) : base(baseValue) { }
        public Vector2MinimumBlender(string name, Vector2 baseValue) : base(name, baseValue) { }

        public sealed override Vector2 Blend(Vector2 a, Vector2 b)
        {
            return Vector2.Min(a, b);
        }
    }

} // namespace UnityExtensions